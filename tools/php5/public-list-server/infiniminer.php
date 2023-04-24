<?php
ob_start();
class InfiniminerPublicServerGame
{
	const gameType_INFINIMINER = "INFINIMINER";
	const regex_gameName = '/^([\w\d\ \(\)\']+)$/S';
	const regex_extraInfo = '/^([\w\d\ \,\!\=]*)$/S';
	const playerCapacity_max = 255;
	const connectPort_min = 5565;
	const connectPort_default = 5565;
	const connectPort_max = 5665;

	protected $ip;
	protected $port;
	protected $name;
	protected $gameType;
	protected $player_count;
	protected $player_capacity;
	protected $extra;

	public function __construct($ip=null,$name='Unamed Server',$gameType='INFINIMINER',$player_count=0,$player_capacity=1,$port=5565,$extra='')
	{
    		$ip              = isset($this->ip) ? $this->ip : $_SERVER['REMOTE_ADDR'];
    		$name            = isset($this->name) ? $this->name : $name;
    		$gameType        = isset($this->gameType) ? $this->gameType : $gameType;
    		$player_count    = isset($this->player_count) ? $this->player_count : $player_count;
    		$player_capacity = isset($this->player_capacity) ? $this->player_capacity : $player_capacity;
    		$port            = isset($this->port) ? $this->port : $port;
    		$extra           = isset($this->extra) ? $this->extra : $extra;
    		if (!filter_var($ip, FILTER_VALIDATE_IP, FILTER_FLAG_IPV4 | FILTER_FLAG_IPV6)) {
        		throw new InvalidArgumentException('Invalid/Unsupported IP Address');
    		}
		else if(preg_match(self::regex_gameName,$name) !== 1)
		{
			throw new InvalidArgumentException('Invalid game name \'' . (string)$name . '\'' );
		}
		else if(preg_match(self::regex_extraInfo,$extra) !== 1)
		{
			throw new InvalidArgumentException('Invalid/Unsupported extra info.');
		}
		else if($player_count < 0)
		{
			throw new InvalidArgumentException('Player count cannot be less than zero.');
		}
		else if($player_count > self::playerCapacity_max)
		{
			throw new InvalidArgumentException('Player count cannot be more than ' . self::playerCapacity_max);
		}
		else if($player_capacity < 1)
		{
			throw new InvalidArgumentException('No point putting a game on the public server if nobody can get in.');
		}
		else if ($player_capacity > self::playerCapacity_max)
		{
			throw new InvalidArgumentException('Can\' have a player capacity greater than ' . self::playerCapacity_max);
		}
		else if($port < self::connectPort_min)
		{
			throw new InvalidArgumentException('Can\'t have a port number lower than ' . self::connectPort_min);
		}
		else if($port > self::connectPort_max)
		{
			throw new InvalidArgumentException('Can\'t have a port number greater than ' . self::connectPort_max);
		}
		else
		{
			switch($gameType)
			{
				case self::gameType_INFINIMINER:
				break;
				default:
					throw new InvalidArgumentException('Unsupported game type!');
				break;
			}
			$this->ip              = $ip;
			$this->name            = $name;
			$this->gameType        = $gameType;
			$this->player_count    = $player_count;
			$this->player_capacity = $player_capacity;
			$this->port            = $port;
			$this->extra           = $extra;
		}
	}

	public function ip()
	{
		return $this->ip;
	}
	public function port()
	{
		return $this->port;
	}
	public function name()
	{
		return $this->name;
	}
	public function gameType()
	{
		return $this->gameType;
	}
	public function playerCount()
	{
		return $this->player_count;
	}
	public function playerCapacity()
	{
		return $this->player_capacity;
	}
	public function extra()
	{
		return $this->extra;
	}
}
abstract class InfiniminerPublicServerList
{
	const sql_getList = 'SELECT game,ip,port,player_count,player_capacity,name,extra FROM infiniminer_games';
	
	private $PDO;
	public static function factory(PDO $PDO)
	{
		switch($PDO->getAttribute(PDO::ATTR_DRIVER_NAME))
		{
			case 'sqlite':
				return new InfiniminerPublicServerList_SQLite($PDO);
			break;
			default:
				throw new InvalidArgumentException('Unsupported database!');
			break;
		}
	}
	protected function sql_createTables()
	{
		return constant(get_class($this) . '::sql_createTables');
	}
	protected function sql_addGame()
	{
		return constant(get_class($this) . '::sql_addGame');
	}
	protected function sql_getList()
	{
		return constant(get_class($this) . '::sql_getList');
	}
	public function __construct(PDO $PDO)
	{
		if($PDO->exec($this->sql_createTables()) === false)
		{
			throw new RuntimeException('Could not setup database!');
		}
		else
		{
			$this->PDO = $PDO;
		}
	}
	public function addOrUpdate(InfiniminerPublicServerGame $game)
	{
		static $sth;
		try {
			if ($sth === null) {
				$sth = $this->PDO->prepare($this->sql_addGame());
				if (!$sth) {
					$errorInfo = $this->PDO->errorInfo();
					throw new RuntimeException('Could not prepare add statement! Error: ' . $errorInfo[2]);
				}
			}
		
			$sth->bindValue(':gameType', $game->gameType(), PDO::PARAM_STR);
			$sth->bindValue(':ip', $game->ip(), PDO::PARAM_STR);
			$sth->bindValue(':port', $game->port(), PDO::PARAM_INT);
			$sth->bindValue(':player_count', $game->playerCount(), PDO::PARAM_INT);
			$sth->bindValue(':player_capacity', $game->playerCapacity(), PDO::PARAM_INT);
			$sth->bindValue(':name', $game->name(), PDO::PARAM_STR);
			$sth->bindValue(':extra', $game->extra(), PDO::PARAM_STR);
		
			if (!$sth->execute()) {
				$errorInfo = $sth->errorInfo(); // Get error information from the query execution
				$errorMessage = isset($errorInfo[2]) ? $errorInfo[2] : 'Unknown error'; // Extract error message, or use a default message if not available
				$exceptionMessage = 'Could not add game to list! Error: ' . $errorMessage; // Append error message to the exception's message
				throw new \RuntimeException($exceptionMessage); // Fully qualify the RuntimeException class
			}
		} catch (PDOException $e) {
			throw new RuntimeException('Error with PDO: ' . $e->getCode());
		}
	}
	public function get()
	{
		static $sth;
		try
		{
			if(isset($sth) === false)
			{
				$sth = $this->PDO->prepare($this->sql_getList());
				if(($sth instanceof PDOStatement) === false)
				{
					throw new RuntimeException('Could not prepare SQL query!');
				}
				else
				{
					$sth->setFetchMode(PDO::FETCH_CLASS,'InfiniminerPublicServerGame');
				}
			}
			$sth->execute();
			$results = $sth->fetchAll();
			return $results;
		}
		catch(PDOException $e)
		{
			throw new RuntimeException('Error with PDO: ' . $e->getCode());
		}
	}
	public static function output($lifetime=null)
	{
		$doc = ob_get_contents();
		ob_clean();
		$ETag = sha1($doc);
		header('Last-Modified:' . gmdate('r',$_SERVER['REQUEST_TIME']));
		$lifetime = isset($lifetime) ? $lifetime : 300;
		header('Expires:' . gmdate('r',$_SERVER['REQUEST_TIME'] + $lifetime));
		header(sprintf('Cache-Control: max-age=%1$u, s-maxage=%1$u, public, must-revalidate, proxy-revalidate',$lifetime));
		if(isset($_SERVER['HTTP_IF_NONE_MATCH']) && $_SERVER['HTTP_IF_NONE_MATCH'] === $ETag)
		{
			header('HTTP/1.1 304 Not Modified');
			exit;
		}
		else
		{
			if(isset($_SERVER['HTTP_ACCEPT_ENCODING']) && strpos($_SERVER['HTTP_ACCEPT_ENCODING'],'gzip') !== false)
			{
				$gzipped = gzencode($doc,9);
				if(strlen($gzipped) < strlen($doc))
				{
					$doc = $gzipped;
					unset($gzipped);
					header('Content-Encoding:gzip');
				}
			}
			header('Content-Length:' . strlen($doc));
			header('ETag:' . $ETag);
			die($doc);
		}
	}
}
/*
class InfiniminerPublicServerList_MySQL
{
	const sql_createTables =
'CREATE TABLE IF NOT EXISTS infiniminer_games (
game ENUM( \'INFINIMINER\' ) NOT NULL ,
ip CHAR( 39 ) NOT NULL ,
port SMALLINT UNSIGNED NOT NULL DEFAULT \'5565\',
player_count TINYINT UNSIGNED NOT NULL DEFAULT \'0\',
player_capacity TINYINT UNSIGNED NOT NULL DEFAULT \'1\',
name CHAR( 255 ) NOT NULL ,
extra CHAR( 255 ) NOT NULL ,
PRIMARY KEY ( game , ip , port )
)';
	const sql_addGame =
'INSERT INTO infiniminer_games (game,ip,port,player_count,player_capacity,name,extra)
VALUES
(
	:gameType,
	:ip,
	:port,
	:player_count,
	:player_capacity,
	:name,
	:extra
)
ON DUPLICATE KEY UPDATE
	player_count=VALUE(player_count),
	player_capacity=VALUE(player_capacity),
	name=VALUES(name),
	extra=VALUES(extra)';
}
*/
class InfiniminerPublicServerList_SQLite extends InfiniminerPublicServerList
{
	const sql_createTables =
"CREATE TABLE IF NOT EXISTS infiniminer_games (
	game CHAR( 255 ) NOT NULL DEFAULT 'INFINIMINER',
	ip CHAR( 39 ) NOT NULL ,
	port SMALLINT UNSIGNED NOT NULL DEFAULT '5565',
	player_count TINYINT UNSIGNED NOT NULL DEFAULT '0',
	player_capacity TINYINT UNSIGNED NOT NULL DEFAULT '1',
	name CHAR( 255 ) NOT NULL ,
	extra CHAR( 255 ) NOT NULL ,
	PRIMARY KEY ( name , ip , port )
)";
const sql_addGame =
"INSERT INTO infiniminer_games (game, ip, port, player_count, player_capacity, name)
VALUES
(
    :gameType,
    :ip,
    :port,
    :player_count,
    :player_capacity,
    :name
)
ON CONFLICT(name, ip, port) DO UPDATE SET
    player_count = :player_count
";
	
}
header('Content-Type:text/plain');
try
{
	$PDO = new PDO('sqlite:infiniminer.sq3');
	$InfiniminerPublicServerList = InfiniminerPublicServerList::factory($PDO);
	switch($_SERVER['REQUEST_METHOD'])
	{
		case 'POST':
			if(isset($_POST['name'],$_POST['game'],$_POST['player_count'],$_POST['player_capacity'],$_POST['extra']))
			{
				$ip = $_SERVER['REMOTE_ADDR'];
				$port = isset($_POST['port']) ? (int)$_POST['port'] : InfiniminerPublicServerGame::connectPort_default;
				$InfiniminerPublicServerList->addOrUpdate(new InfiniminerPublicServerGame($ip,$_POST['name'],$_POST['game'],$_POST['player_count'],$_POST['player_capacity'],$port,$_POST['extra']));
			}
			else
			{
				header('HTTP/1.1 400 Bad Request');
				echo 'You forgot some required parameters!';
			}
		break;
		case 'GET':
			$games = $InfiniminerPublicServerList->get();
			if(empty($games) === false)
			{
				foreach($games as $game)
				{
					$extra = explode(',',$game->extra());
					$extra[] = ' port=' . $game->port();
					$extra = implode(',',$extra);
					$data = array(
  						'name' => $game->name(),
  						'ip' => $game->ip(),
  						'gameType' => $game->gameType(),
  						'playerCount' => $game->playerCount(),
  						'playerCapacity' => $game->playerCapacity(),
  						'extra' => $game->extra()
					);
					$jsonData = json_encode($data);
					echo $jsonData;
				}
			}
		break;
		default:
			header('HTTP/1.1 405 Method Not Allowed');
			exit;
		break;
	}
}
catch(Exception $e)
{
	header('HTTP/1.1 500 Internal Server Error');
	echo 'ERROR !:',get_class($e),"\n",$e->getMessage();
}
InfiniminerPublicServerList::output();
?>
