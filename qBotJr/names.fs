﻿namespace qBotJr

open System

module names =

  let private adjs =
    [|
      "Adorable"
      "Adventurous"
      "Aged"
      "Aggressive"
      "Agreeable"
      "Alert"
      "Alive"
      "Amused"
      "Ancient"
      "Angry"
      "Annoyed"
      "Annoying"
      "Anxious"
      "Arrogant"
      "Ashamed"
      "Attractive"
      "Autumn"
      "Average"
      "Awful"
      "Bad"
      "Beautiful"
      "Better"
      "Bewildered"
      "Billowing"
      "Bitter"
      "Black"
      "Black"
      "Bloody"
      "Blue"
      "Blue"
      "Blue-eyed"
      "Blushing"
      "Bold"
      "Bored"
      "Brainy"
      "Brave"
      "Breakable"
      "Bright"
      "Broken"
      "Busy"
      "Calm"
      "Careful"
      "Cautious"
      "Charming"
      "Cheerful"
      "Clean"
      "Clear"
      "Clever"
      "Cloudy"
      "Clumsy"
      "Cold"
      "Colorful"
      "Combative"
      "Comfortable"
      "Concerned"
      "Condemned"
      "Confused"
      "Cool"
      "Cooperative"
      "Courageous"
      "Crazy"
      "Creepy"
      "Crimson"
      "Crowded"
      "Cruel"
      "Curious"
      "Cute"
      "Damp"
      "Dangerous"
      "Dark"
      "Dark"
      "Dawn"
      "Dead"
      "Defeated"
      "Defiant"
      "Delicate"
      "Delightful"
      "Depressed"
      "Determined"
      "Different"
      "Difficult"
      "Disgusted"
      "Distinct"
      "Disturbed"
      "Divine"
      "Dizzy"
      "Doubtful"
      "Drab"
      "Dry"
      "Dull"
      "Eager"
      "Easy"
      "Elated"
      "Elegant"
      "Embarrassed"
      "Empty"
      "Enchanting"
      "Encouraging"
      "Energetic"
      "Enthusiastic"
      "Envious"
      "Evil"
      "Excited"
      "Expensive"
      "Exuberant"
      "Fair"
      "Faithful"
      "Falling"
      "Famous"
      "Fancy"
      "Fantastic"
      "Fierce"
      "Filthy"
      "Fine"
      "Floral"
      "Foolish"
      "Fragile"
      "Fragrant"
      "Frail"
      "Frantic"
      "Friendly"
      "Frightened"
      "Frosty"
      "Funny"
      "Gentle"
      "Gifted"
      "Glamorous"
      "Gleaming"
      "Glorious"
      "Good"
      "Gorgeous"
      "Graceful"
      "Green"
      "Grieving"
      "Grotesque"
      "Grumpy"
      "Handsome"
      "Happy"
      "Healthy"
      "Helpful"
      "Helpless"
      "Hidden"
      "Hilarious"
      "Holy"
      "Homeless"
      "Homely"
      "Horrible"
      "Hungry"
      "Hurt"
      "Icy"
      "Ill"
      "Important"
      "Impossible"
      "Inexpensive"
      "Innocent"
      "Inquisitive"
      "Itchy"
      "Jealous"
      "Jittery"
      "Jolly"
      "Joyous"
      "Kind"
      "Late"
      "Lazy"
      "Light"
      "Lingering"
      "Little"
      "Lively"
      "Lively"
      "Lonely"
      "Long"
      "Long"
      "Lovely"
      "Lucky"
      "Magnificent"
      "Misty"
      "Misty"
      "Modern"
      "Morning"
      "Motionless"
      "Muddy"
      "Muddy"
      "Mushy"
      "Mysterious"
      "Nameless"
      "Nasty"
      "Naughty"
      "Nervous"
      "Nice"
      "Nutty"
      "Obedient"
      "Obnoxious"
      "Odd"
      "Old"
      "Old-fashioned"
      "Open"
      "Outrageous"
      "Outstanding"
      "Panicky"
      "Patient"
      "Perfect"
      "Plain"
      "Pleasant"
      "Poised"
      "Polished"
      "Poor"
      "Powerful"
      "Precious"
      "Prickly"
      "Proud"
      "Purple"
      "Puzzled"
      "Quaint"
      "Quiet"
      "Real"
      "Red"
      "Relieved"
      "Repulsive"
      "Restless"
      "Rich"
      "Rough"
      "Scary"
      "Selfish"
      "Shiny"
      "Shy"
      "Shy"
      "Silent"
      "Silly"
      "Sleepy"
      "Small"
      "Smiling"
      "Smoggy"
      "Snowy"
      "Solitary"
      "Sore"
      "Sparkling"
      "Splendid"
      "Spotless"
      "Spring"
      "Still"
      "Stormy"
      "Strange"
      "Stupid"
      "Successful"
      "Summer"
      "Super"
      "Talented"
      "Tame"
      "Tender"
      "Tense"
      "Terrible"
      "Testy"
      "Thankful"
      "Thoughtful"
      "Thoughtless"
      "Throbbing"
      "Tired"
      "Tough"
      "Troubled"
      "Twilight"
      "Ugliest"
      "Ugly"
      "Uninterested"
      "Unsightly"
      "Unusual"
      "Upset"
      "Uptight"
      "Vast"
      "Victorious"
      "Vivacious"
      "Wandering"
      "Weary"
      "Weathered"
      "White"
      "Wicked"
      "Wide-eyed"
      "Wild"
      "Winter"
      "Wispy"
      "Withered"
      "Witty"
      "Worried"
      "Worrisome"
      "Wrong"
      "Xenophobic"
      "Yawning"
      "Yellowed"
      "Young"
      "Yucky"
      "Zany"
      "Zealous"
    |]

  let private nouns =
    [|
      "Aardvark"
      "Addax"
      "Albatross"
      "Alligator"
      "Alpaca"
      "Anaconda"
      "Angelfish"
      "Ant"
      "Anteater"
      "Antelope"
      "Ape"
      "Armadillo"
      "Baboon"
      "Badger"
      "Barracuda"
      "Bat"
      "Batfish"
      "Bear"
      "Beaver"
      "Bee"
      "Beetle"
      "Bird"
      "Bird"
      "Bison"
      "Boar"
      "Booby"
      "Breeze"
      "Brook"
      "Buffalo"
      "Bug"
      "Bush"
      "Butterfly"
      "Butterfly"
      "Buzzard"
      "Caiman"
      "Camel"
      "Capuchin"
      "Capybara"
      "Caracal"
      "Cardinal"
      "Caribou"
      "Cassowary"
      "Cat"
      "Caterpillar"
      "Centipede"
      "Chamois"
      "Cheetah"
      "Cherry"
      "Chicken"
      "Chimpanzee"
      "Chinchilla"
      "Chipmunk"
      "Cicada"
      "Civet"
      "Cloud"
      "Cobra"
      "Cockroach"
      "Cod"
      "Constrictor"
      "Copperhead"
      "Cormorant"
      "Corncrake"
      "Cottonmouth"
      "Cow"
      "Cowfish"
      "Coyote"
      "Crab"
      "Crane"
      "Crayfish"
      "Crocodile"
      "Crossbill"
      "Curlew"
      "Darkness"
      "Dawn"
      "Deer"
      "Dew"
      "Dingo"
      "Dog"
      "Dogfish"
      "Dolphin"
      "Donkey"
      "Dormouse"
      "Dotterel"
      "Dove"
      "Dragonfly"
      "Dream"
      "Duck"
      "Dugong"
      "Dunlin"
      "Dust"
      "Eagle"
      "Earthworm"
      "Echidna"
      "Eel"
      "Eland"
      "Elephant"
      "Elk"
      "Emu"
      "Falcon"
      "Feather"
      "Ferret"
      "Field"
      "Finch"
      "Fire"
      "Firefly"
      "Fish"
      "Flamingo"
      "Flatworm"
      "Flower"
      "Fly"
      "Fog"
      "Forest"
      "Fowl"
      "Fox"
      "Frog"
      "Frog"
      "Frost"
      "Gannet"
      "Gaur"
      "Gazelle"
      "Gecko"
      "Gemsbok"
      "Gentoo"
      "Gerbil"
      "Gerenuk"
      "Gharial"
      "Gibbon"
      "Giraffe"
      "Glade"
      "Glitter"
      "Gnat"
      "Gnu"
      "Goat"
      "Goldfinch"
      "Goosander"
      "Goose"
      "Gorilla"
      "Goshawk"
      "Grass"
      "Grasshopper"
      "Grebe"
      "Grivet"
      "Grouse"
      "Guanaco"
      "Gull"
      "Hamerkop"
      "Hamster"
      "Hare"
      "Hawk"
      "Haze"
      "Hedgehog"
      "Heron"
      "Herring"
      "Hill"
      "Hippopotamus"
      "Hoopoe"
      "Hornet"
      "Horse"
      "Hummingbird"
      "Hyena"
      "Ibex"
      "Ibis"
      "Iguana"
      "Impala"
      "Jackal"
      "Jaguar"
      "Jay"
      "Jellyfish"
      "Kangaroo"
      "Katipo"
      "Kea"
      "Kestrel"
      "Kingfisher"
      "Kinkajou"
      "Kitten"
      "Koala"
      "Kookaburra"
      "Kouprey"
      "Kudu"
      "Ladybird"
      "Lake"
      "Lapwing"
      "Lark"
      "Leaf"
      "Lemur"
      "Leopard"
      "Lion"
      "Lizard"
      "Llama"
      "Lobster"
      "Locust"
      "Loris"
      "Louse"
      "Lynx"
      "Lyrebird"
      "Macaque"
      "Macaw"
      "Magpie"
      "Mallard"
      "Mamba"
      "Manatee"
      "Mandrill"
      "Mantis"
      "Manx"
      "Markhor"
      "Marten"
      "Meadow"
      "Meerkat"
      "Millipede"
      "Mink"
      "Mockingbird"
      "Mole"
      "Mongoose"
      "Monkey"
      "Moon"
      "Moose"
      "Morning"
      "Mosquito"
      "Moth"
      "Mountain"
      "Mouse"
      "Narwhal"
      "Newt"
      "Night"
      "Nightingale"
      "Ocelot"
      "Octopus"
      "Okapi"
      "Opossum"
      "Orangutan"
      "Oryx"
      "Osprey"
      "Ostrich"
      "Otter"
      "Owl"
      "Ox"
      "Oyster"
      "Oystercatcher"
      "Panda"
      "Panther"
      "Paper"
      "Parrot"
      "Partridge"
      "Peacock"
      "Peafowl"
      "Peccary"
      "Pelican"
      "Penguin"
      "Petrel"
      "Pheasant"
      "Pig"
      "Pigeon"
      "Pine"
      "Pintail"
      "Piranha"
      "Platypus"
      "Plover"
      "Polecat"
      "Pollan"
      "Pond"
      "Pony"
      "Porcupine"
      "Porpoise"
      "Puffin"
      "Puma"
      "Pygmy"
      "Quagga"
      "Quail"
      "Quelea"
      "Quetzal"
      "Quoll"
      "Rabbit"
      "Raccoon"
      "Rain"
      "Rat"
      "Ratel"
      "Rattlesnake"
      "Raven"
      "Ray"
      "Reindeer"
      "Resonance"
      "Rhinoceros"
      "River"
      "Rook"
      "Sable"
      "Salamander"
      "Salmon"
      "Sandpiper"
      "Sardine"
      "Scarab"
      "Sea"
      "Seahorse"
      "Seal"
      "Serval"
      "Shadow"
      "Shape"
      "Shark"
      "Sheep"
      "Shrew"
      "Shrike"
      "Silence"
      "Skimmer"
      "Skipper"
      "Skunk"
      "Sky"
      "Skylark"
      "Sloth"
      "Smoke"
      "Snail"
      "Snake"
      "Snow"
      "Snowflake"
      "Sound"
      "Spider"
      "Squirrel"
      "Stag"
      "Star"
      "Starling"
      "Stoat"
      "Stork"
      "Sun"
      "Sun"
      "Sunset"
      "Surf"
      "Swan"
      "Swiftlet"
      "Tamarin"
      "Tapir"
      "Tarantula"
      "Tarsier"
      "Teira"
      "Termite"
      "Tern"
      "Thrush"
      "Thunder"
      "Tiger"
      "Toad"
      "Tortoise"
      "Toucan"
      "Tree"
      "Trout"
      "Tuatara"
      "Turkey"
      "Turtle"
      "Unicorn"
      "Vendace"
      "Vicuña"
      "Violet"
      "Voice"
      "Vole"
      "Vulture"
      "Wallaby"
      "Walrus"
      "Warbler"
      "Wasp"
      "Water"
      "Water"
      "Waterfall"
      "Wave"
      "Weasel"
      "Weevil"
      "Whale"
      "Wildebeest"
      "Wildflower"
      "Willet"
      "Wind"
      "Wolf"
      "Wolverine"
      "Wombat"
      "Wood"
      "Worm"
      "Wren"
      "Wryneck"
      "Xenomorph"
      "Yacare"
      "Yak"
      "Zebra"

    |]

  let private rand = Random(DateTime.Now.Millisecond)

  let private adjMax = adjs.Length - 1
  let private nounMax = nouns.Length - 1
  let getRand() =
    let ai = rand.Next(0, adjMax)
    let ni = rand.Next(0, nounMax)
    adjs.[ai] + nouns.[ni]




