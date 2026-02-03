// Undercover Barber - A Cop's Cut
// Main Game Logic

const GameState = {
    TITLE: 'title',
    BRIEFING: 'briefing',
    BARBERSHOP: 'barbershop',
    STREET_CHASE: 'street_chase',
    CAR_CHASE: 'car_chase',
    RESULT: 'result'
};

// Suspect profiles - one will be chosen as the real suspect
const SUSPECT_PROFILES = [
    {
        codename: "The Clipper",
        traits: [
            "Has a distinctive scar on left cheek",
            "Always asks about 'the old neighborhood'",
            "Nervous around police sirens",
            "Tips excessively with cash"
        ],
        activity: "Suspected of running an underground gambling ring. Last seen near the docks."
    },
    {
        codename: "Slick Eddie",
        traits: [
            "Wears expensive watches",
            "Constantly checks phone",
            "Mentions 'business meetings' frequently",
            "Has a slight foreign accent"
        ],
        activity: "Money laundering through local businesses. Known to use fake identities."
    },
    {
        codename: "The Professor",
        traits: [
            "Uses overly formal language",
            "References obscure historical events",
            "Avoids eye contact when lying",
            "Always requests the same specific haircut"
        ],
        activity: "Art theft ring mastermind. Believed to be planning a major heist."
    }
];

// Customer templates
const CUSTOMER_TEMPLATES = [
    {
        name: "Mike Thompson",
        avatar: "👨",
        personality: "friendly",
        haircutRequest: "Just a trim, nothing fancy. Keep it professional.",
        dialoguePool: {
            smalltalk: [
                "Beautiful weather we're having, eh?",
                "Did you catch the game last night?",
                "Been coming to barbershops since I was a kid."
            ],
            probe: [
                "Me? I work in accounting. Nothing exciting.",
                "Live just down the street, actually.",
                "Married, two kids. They keep me busy!"
            ],
            direct: [
                "Suspicious? Nah, I'm an open book!",
                "Ha! The most illegal thing I do is jaywalk.",
                "Why so many questions? Just here for a haircut, friend."
            ]
        },
        suspectTraitMatches: 0
    },
    {
        name: "Tony Deluca",
        avatar: "👴",
        personality: "gruff",
        haircutRequest: "The usual. You know how I like it. Clean on the sides.",
        dialoguePool: {
            smalltalk: [
                "*grunts* Yeah, weather's fine I guess.",
                "Don't watch sports anymore. Waste of time.",
                "Been getting haircuts here for 30 years."
            ],
            probe: [
                "Retired. Used to work construction.",
                "What's it to ya where I live?",
                "Mind your own business, alright?"
            ],
            direct: [
                "You asking a lot of questions for a barber.",
                "I don't like your tone, kid.",
                "Just cut my hair and we're done here."
            ]
        },
        suspectTraitMatches: 0
    },
    {
        name: "Derek Williams",
        avatar: "🧔",
        personality: "nervous",
        haircutRequest: "Uh, something that looks... professional? I have a big meeting.",
        dialoguePool: {
            smalltalk: [
                "Yeah... yeah the weather is nice. *looks around*",
                "Sports? Oh, sometimes, when I have time...",
                "First time at this shop actually."
            ],
            probe: [
                "I'm... in sales. Various things.",
                "I move around a lot for work, you know?",
                "Single. Too busy for relationships."
            ],
            direct: [
                "*sweating* Why would you ask that?",
                "I-I'm not suspicious! Why would I be?",
                "Look, can we just... focus on the haircut?"
            ]
        },
        suspectTraitMatches: 0
    },
    {
        name: "James Chen",
        avatar: "👨‍💼",
        personality: "confident",
        haircutRequest: "Executive cut. I have business meetings all week.",
        dialoguePool: {
            smalltalk: [
                "Perfect day for closing deals!",
                "I prefer golf to watching sports.",
                "A good barber is essential for success."
            ],
            probe: [
                "Finance sector. Can't discuss specifics, NDAs.",
                "I have places downtown and in the suburbs.",
                "Dating? No time. Money doesn't sleep."
            ],
            direct: [
                "Interesting question. Very interesting.",
                "Everyone has secrets. What matters is results.",
                "I answer to my shareholders, not barbers."
            ]
        },
        suspectTraitMatches: 0
    },
    {
        name: "Robert Martinez",
        avatar: "👨‍🦱",
        personality: "chatty",
        haircutRequest: "Fade on the sides, keep some length on top!",
        dialoguePool: {
            smalltalk: [
                "Man, this weather reminds me of my hometown!",
                "Bro, that game was INSANE last night!",
                "Love this shop, great vibes here."
            ],
            probe: [
                "I'm a DJ! Club Velvet, come check it out!",
                "Got a place near the old neighborhood.",
                "Playing the field, you know how it is!"
            ],
            direct: [
                "Suspicious? Nah man, I'm just vibing!",
                "Haha, you'd make a good cop with those questions!",
                "Look, I just spin records, nothing shady."
            ]
        },
        suspectTraitMatches: 0
    }
];

// Game class
class UndercoverBarberGame {
    constructor() {
        this.currentState = GameState.TITLE;
        this.suspectProfile = null;
        this.customers = [];
        this.currentCustomerIndex = -1;
        this.currentCustomer = null;
        this.haircutProgress = 0;
        this.reputation = 3;
        this.suspicionLevel = 0;
        this.dialogueCount = 0;
        this.trueSuspectIndex = -1;
        this.playerGuess = -1;
        this.caughtCorrectSuspect = false;

        // Chase game state
        this.chaseDistance = 100;
        this.stamina = 100;
        this.carHealth = 100;
        this.playerLane = 1;
        this.chaseAnimationId = null;

        this.init();
    }

    init() {
        this.setupEventListeners();
        this.showScreen(GameState.TITLE);
    }

    setupEventListeners() {
        // Title screen
        document.getElementById('start-btn').addEventListener('click', () => {
            this.startBriefing();
        });

        // Briefing screen
        document.getElementById('begin-mission-btn').addEventListener('click', () => {
            this.startMission();
        });

        // Barbershop controls
        document.getElementById('next-customer-btn').addEventListener('click', () => {
            this.nextCustomer();
        });

        document.getElementById('suspect-btn').addEventListener('click', () => {
            this.identifySuspect();
        });

        // Dialogue buttons
        document.querySelectorAll('.dialogue-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                this.handleDialogue(e.target.dataset.type);
            });
        });

        // Tool selection
        document.querySelectorAll('.tool-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                document.querySelectorAll('.tool-btn').forEach(b => b.classList.remove('active'));
                e.target.classList.add('active');
            });
        });

        // Haircut canvas
        const canvas = document.getElementById('hair-canvas');
        let isDrawing = false;

        canvas.addEventListener('mousedown', () => isDrawing = true);
        canvas.addEventListener('mouseup', () => isDrawing = false);
        canvas.addEventListener('mouseleave', () => isDrawing = false);
        canvas.addEventListener('mousemove', (e) => {
            if (isDrawing) this.cutHair(e);
        });

        // Touch support for mobile
        canvas.addEventListener('touchstart', (e) => {
            isDrawing = true;
            this.cutHair(e.touches[0]);
        });
        canvas.addEventListener('touchend', () => isDrawing = false);
        canvas.addEventListener('touchmove', (e) => {
            if (isDrawing) {
                e.preventDefault();
                this.cutHair(e.touches[0]);
            }
        });

        // Chase controls
        document.querySelectorAll('.chase-btn[data-dir]').forEach(btn => {
            btn.addEventListener('click', (e) => {
                this.handleChaseInput(e.target.dataset.dir);
            });
        });

        document.querySelectorAll('.chase-btn[data-lane]').forEach(btn => {
            btn.addEventListener('click', (e) => {
                this.changeLane(parseInt(e.target.dataset.lane));
            });
        });

        document.getElementById('sprint-btn')?.addEventListener('click', () => {
            this.sprint();
        });

        document.getElementById('nitro-btn')?.addEventListener('click', () => {
            this.useNitro();
        });

        // Keyboard controls for chase
        document.addEventListener('keydown', (e) => {
            if (this.currentState === GameState.STREET_CHASE) {
                if (e.key === 'ArrowLeft') this.handleChaseInput('left');
                if (e.key === 'ArrowRight') this.handleChaseInput('right');
                if (e.key === 'ArrowUp' || e.key === ' ') this.handleChaseInput('jump');
            } else if (this.currentState === GameState.CAR_CHASE) {
                if (e.key === 'ArrowLeft' || e.key === 'a') this.changeLane(Math.max(0, this.playerLane - 1));
                if (e.key === 'ArrowRight' || e.key === 'd') this.changeLane(Math.min(2, this.playerLane + 1));
                if (e.key === ' ') this.useNitro();
            }
        });

        // Play again
        document.getElementById('play-again-btn').addEventListener('click', () => {
            this.resetGame();
        });
    }

    showScreen(state) {
        document.querySelectorAll('.screen').forEach(s => s.classList.remove('active'));

        let screenId;
        switch (state) {
            case GameState.TITLE: screenId = 'title-screen'; break;
            case GameState.BRIEFING: screenId = 'briefing-screen'; break;
            case GameState.BARBERSHOP: screenId = 'barbershop-screen'; break;
            case GameState.STREET_CHASE: screenId = 'street-chase-screen'; break;
            case GameState.CAR_CHASE: screenId = 'car-chase-screen'; break;
            case GameState.RESULT: screenId = 'result-screen'; break;
        }

        document.getElementById(screenId).classList.add('active');
        this.currentState = state;
    }

    startBriefing() {
        // Select a random suspect profile
        this.suspectProfile = SUSPECT_PROFILES[Math.floor(Math.random() * SUSPECT_PROFILES.length)];

        // Update briefing screen
        document.getElementById('suspect-codename').textContent = this.suspectProfile.codename;

        const traitsList = document.getElementById('suspect-traits');
        traitsList.innerHTML = '';
        this.suspectProfile.traits.forEach(trait => {
            const li = document.createElement('li');
            li.textContent = trait;
            traitsList.appendChild(li);
        });

        document.getElementById('suspect-activity').textContent = this.suspectProfile.activity;

        // Generate customers and assign true suspect
        this.generateCustomers();

        this.showScreen(GameState.BRIEFING);
    }

    generateCustomers() {
        // Shuffle and pick 5 customers
        const shuffled = [...CUSTOMER_TEMPLATES].sort(() => Math.random() - 0.5);
        this.customers = shuffled.slice(0, 5).map(c => ({...c}));

        // Randomly assign one as the true suspect
        this.trueSuspectIndex = Math.floor(Math.random() * this.customers.length);

        // Modify the true suspect's dialogue to match traits
        const suspect = this.customers[this.trueSuspectIndex];
        suspect.isSuspect = true;
        suspect.suspectTraitMatches = this.suspectProfile.traits.length;

        // Add suspicious dialogue based on profile
        this.addSuspectClues(suspect);
    }

    addSuspectClues(suspect) {
        const profile = this.suspectProfile;

        // Add trait-revealing dialogue
        if (profile.codename === "The Clipper") {
            suspect.dialoguePool.smalltalk.push("*touches scar on cheek* Got this in the old neighborhood...");
            suspect.dialoguePool.probe.push("The old neighborhood... those were different times.");
            suspect.dialoguePool.direct.push("*flinches at distant siren* ...What? Nothing. Just the haircut.");
            suspect.haircutRequest = "Clean it up. And here's a little extra for your trouble. *hands over large cash*";
        } else if (profile.codename === "Slick Eddie") {
            suspect.dialoguePool.smalltalk.push("*checks expensive watch* Time is money, as they say.");
            suspect.dialoguePool.probe.push("Business meetings, always business meetings. *checks phone again*");
            suspect.dialoguePool.direct.push("Identity? *slight accent slips out* I am who I need to be.");
            suspect.avatar = "🕴️";
        } else if (profile.codename === "The Professor") {
            suspect.dialoguePool.smalltalk.push("Did you know this building dates back to 1923? Fascinating architecture.");
            suspect.dialoguePool.probe.push("*avoids eye contact* My work? Academic pursuits, nothing more.");
            suspect.dialoguePool.direct.push("*stares at wall* Give me the usual. Same cut. Exactly 2 inches off the sides.");
            suspect.haircutRequest = "The usual. Exactly 2 inches off the sides, 1.5 on top. Precisely.";
        }
    }

    startMission() {
        this.currentCustomerIndex = -1;
        document.getElementById('customer-count').textContent = '0';
        document.getElementById('dialogue-history').innerHTML = '';
        this.showScreen(GameState.BARBERSHOP);

        // Show initial message
        this.addDialogue('system', "Welcome to Clip Joint. Your first customer will arrive shortly...");

        setTimeout(() => {
            this.nextCustomer();
        }, 1500);
    }

    nextCustomer() {
        this.currentCustomerIndex++;

        if (this.currentCustomerIndex >= this.customers.length) {
            // All customers served, must make a decision
            this.addDialogue('system', "That's all the customers for today. Time to make your move or let them go...");
            document.getElementById('next-customer-btn').classList.add('hidden');
            return;
        }

        this.currentCustomer = this.customers[this.currentCustomerIndex];
        this.haircutProgress = 0;
        this.dialogueCount = 0;
        this.suspicionLevel = 0;

        // Update UI
        document.getElementById('customer-count').textContent = this.currentCustomerIndex + 1;
        document.getElementById('customer-avatar').textContent = this.currentCustomer.avatar;
        document.getElementById('customer-name').textContent = this.currentCustomer.name;
        document.getElementById('haircut-area').classList.remove('hidden');
        document.getElementById('suspect-btn').classList.remove('hidden');
        document.getElementById('cut-progress-fill').style.width = '0%';

        // Clear dialogue and show customer's request
        document.getElementById('dialogue-history').innerHTML = '';
        this.addDialogue('customer', `"${this.currentCustomer.haircutRequest}"`);

        // Initialize hair canvas
        this.initHairCanvas();

        // Enable dialogue buttons
        document.querySelectorAll('.dialogue-btn').forEach(btn => btn.disabled = false);
    }

    initHairCanvas() {
        const canvas = document.getElementById('hair-canvas');
        const ctx = canvas.getContext('2d');

        // Draw head
        ctx.fillStyle = '#f4d03f';
        ctx.beginPath();
        ctx.arc(150, 170, 100, 0, Math.PI * 2);
        ctx.fill();

        // Draw hair (to be "cut")
        ctx.fillStyle = '#333';
        this.hairPixels = [];

        for (let i = 0; i < 500; i++) {
            const angle = Math.random() * Math.PI;
            const distance = 80 + Math.random() * 40;
            const x = 150 + Math.cos(angle) * distance;
            const y = 150 - Math.sin(angle) * distance;

            this.hairPixels.push({ x, y, cut: false });
            ctx.fillRect(x - 3, y - 3, 6, 6);
        }

        // Draw face features
        ctx.fillStyle = '#333';
        ctx.beginPath();
        ctx.arc(120, 160, 5, 0, Math.PI * 2); // Left eye
        ctx.arc(180, 160, 5, 0, Math.PI * 2); // Right eye
        ctx.fill();

        ctx.beginPath();
        ctx.arc(150, 200, 20, 0, Math.PI); // Smile
        ctx.stroke();
    }

    cutHair(e) {
        if (!this.currentCustomer) return;

        const canvas = document.getElementById('hair-canvas');
        const rect = canvas.getBoundingClientRect();
        const x = (e.clientX || e.pageX) - rect.left;
        const y = (e.clientY || e.pageY) - rect.top;
        const ctx = canvas.getContext('2d');

        // Scale for canvas size
        const scaleX = canvas.width / rect.width;
        const scaleY = canvas.height / rect.height;
        const canvasX = x * scaleX;
        const canvasY = y * scaleY;

        // Check if cutting hair
        let cutCount = 0;
        this.hairPixels.forEach(pixel => {
            if (!pixel.cut) {
                const dist = Math.sqrt((pixel.x - canvasX) ** 2 + (pixel.y - canvasY) ** 2);
                if (dist < 20) {
                    pixel.cut = true;
                    cutCount++;

                    // Erase hair
                    ctx.fillStyle = '#f4d03f';
                    ctx.fillRect(pixel.x - 4, pixel.y - 4, 8, 8);
                }
            }
        });

        // Update progress
        const cutPixels = this.hairPixels.filter(p => p.cut).length;
        this.haircutProgress = Math.min(100, (cutPixels / this.hairPixels.length) * 100 * 2);
        document.getElementById('cut-progress-fill').style.width = `${this.haircutProgress}%`;

        if (this.haircutProgress >= 100 && !this.currentCustomer.haircutComplete) {
            this.currentCustomer.haircutComplete = true;
            this.addDialogue('customer', "Looking good! Thanks for the cut.");
            this.addDialogue('system', "Haircut complete! Finish your conversation or move to the next customer.");
        }
    }

    handleDialogue(type) {
        if (!this.currentCustomer) return;

        this.dialogueCount++;
        const customer = this.currentCustomer;
        const pool = customer.dialoguePool[type];

        // Player's line
        let playerLine;
        switch (type) {
            case 'smalltalk':
                playerLine = ["So, nice weather today...", "Catch any good games lately?", "Come here often?"][Math.floor(Math.random() * 3)];
                break;
            case 'probe':
                playerLine = ["What do you do for work?", "You from around here?", "Family man?"][Math.floor(Math.random() * 3)];
                this.suspicionLevel += 10;
                break;
            case 'direct':
                playerLine = ["You seem a bit on edge...", "You look familiar. Been in trouble before?", "What brings you to this neighborhood?"][Math.floor(Math.random() * 3)];
                this.suspicionLevel += 25;
                break;
        }

        this.addDialogue('player', playerLine);

        // Customer response after delay
        setTimeout(() => {
            const response = pool[Math.floor(Math.random() * pool.length)];
            this.addDialogue('customer', response);

            // Check if customer is getting suspicious of the cop
            if (this.suspicionLevel > 50 && customer.isSuspect) {
                setTimeout(() => {
                    this.addDialogue('customer', "*eyes narrow* You ask a lot of questions for a barber...");
                }, 1000);
            }

            // Add player thoughts about suspect traits
            if (customer.isSuspect && this.dialogueCount >= 2) {
                this.showPlayerThought();
            }
        }, 500);

        // Limit dialogue per customer
        if (this.dialogueCount >= 5) {
            document.querySelectorAll('.dialogue-btn').forEach(btn => btn.disabled = true);
            this.addDialogue('system', "Time to make a decision about this customer...");
        }
    }

    showPlayerThought() {
        const thoughts = [
            "Hmm, something about this one...",
            "Wait, that matches the briefing...",
            "Could this be our suspect?",
            "That trait seems familiar..."
        ];

        const existingThought = document.querySelector('.thought-bubble');
        if (existingThought) existingThought.remove();

        const thought = document.createElement('div');
        thought.className = 'thought-bubble';
        thought.textContent = thoughts[Math.floor(Math.random() * thoughts.length)];
        document.querySelector('.barber-chair-area').appendChild(thought);

        setTimeout(() => thought.remove(), 3000);
    }

    addDialogue(speaker, text) {
        const history = document.getElementById('dialogue-history');
        const entry = document.createElement('div');
        entry.className = `dialogue-entry ${speaker}`;

        let speakerName;
        switch (speaker) {
            case 'customer': speakerName = this.currentCustomer?.name || 'Customer'; break;
            case 'player': speakerName = 'You (undercover)'; break;
            case 'system': speakerName = ''; break;
        }

        if (speakerName) {
            const nameSpan = document.createElement('div');
            nameSpan.className = 'speaker';
            nameSpan.textContent = speakerName;
            entry.appendChild(nameSpan);
        }

        const textSpan = document.createElement('div');
        textSpan.textContent = text;
        if (speaker === 'system') {
            textSpan.style.fontStyle = 'italic';
            textSpan.style.color = '#888';
        }
        entry.appendChild(textSpan);

        history.appendChild(entry);
        history.scrollTop = history.scrollHeight;
    }

    identifySuspect() {
        if (!this.currentCustomer) return;

        this.playerGuess = this.currentCustomerIndex;
        this.caughtCorrectSuspect = this.currentCustomer.isSuspect;

        if (this.caughtCorrectSuspect) {
            this.addDialogue('player', "FREEZE! Police! You're under arrest!");
            this.addDialogue('customer', "What?! You're a cop?! *knocks chair over and runs*");

            setTimeout(() => {
                this.startStreetChase();
            }, 2000);
        } else {
            // Wrong suspect
            this.addDialogue('player', "Hold it right there!");
            this.addDialogue('customer', "What?! What did I do?!");
            this.addDialogue('system', "This customer is innocent. You've made a mistake...");

            this.reputation--;
            this.updateReputation();

            if (this.reputation <= 0) {
                setTimeout(() => {
                    this.showResult(false, "Your cover is blown! Too many false accusations.");
                }, 2000);
            } else {
                setTimeout(() => {
                    this.addDialogue('system', "Apologize and continue. The real suspect is still out there.");
                    this.nextCustomer();
                }, 2000);
            }
        }
    }

    updateReputation() {
        const stars = '⭐'.repeat(Math.max(0, this.reputation));
        document.getElementById('reputation').textContent = stars || '💀';
    }

    // STREET CHASE
    startStreetChase() {
        this.showScreen(GameState.STREET_CHASE);
        this.chaseDistance = 100;
        this.stamina = 100;

        const cop = document.getElementById('cop-sprite');
        const suspect = document.getElementById('suspect-sprite');

        cop.style.left = '30%';
        cop.style.bottom = '20%';
        suspect.style.left = '50%';
        suspect.style.bottom = '60%';

        this.updateChaseUI();
        this.startObstacles();
        this.runStreetChase();
    }

    runStreetChase() {
        const gameLoop = () => {
            if (this.currentState !== GameState.STREET_CHASE) return;

            // Reduce distance over time (suspect getting tired)
            this.chaseDistance -= 0.1;

            // Regenerate stamina slowly
            this.stamina = Math.min(100, this.stamina + 0.05);

            this.updateChaseUI();

            if (this.chaseDistance <= 0) {
                this.winStreetChase();
                return;
            }

            this.chaseAnimationId = requestAnimationFrame(gameLoop);
        };

        this.chaseAnimationId = requestAnimationFrame(gameLoop);
    }

    startObstacles() {
        if (this.currentState !== GameState.STREET_CHASE) return;

        const container = document.getElementById('obstacles-container');
        const obstacles = ['🗑️', '🚧', '📦', '🛒', '🚲'];

        const obstacle = document.createElement('div');
        obstacle.className = 'obstacle';
        obstacle.textContent = obstacles[Math.floor(Math.random() * obstacles.length)];
        obstacle.style.left = `${20 + Math.random() * 60}%`;

        container.appendChild(obstacle);

        obstacle.addEventListener('animationend', () => {
            obstacle.remove();
        });

        // Check for collision
        setTimeout(() => {
            if (this.currentState === GameState.STREET_CHASE) {
                const copPos = document.getElementById('cop-sprite').offsetLeft;
                const obsPos = obstacle.offsetLeft;

                if (Math.abs(copPos - obsPos) < 50) {
                    // Hit obstacle
                    this.chaseDistance += 10;
                    this.stamina -= 15;
                    document.getElementById('cop-sprite').textContent = '🤕';
                    setTimeout(() => {
                        document.getElementById('cop-sprite').textContent = '🏃';
                    }, 500);
                }
            }
        }, 1500);

        // Continue spawning
        setTimeout(() => this.startObstacles(), 1500 + Math.random() * 1000);
    }

    handleChaseInput(dir) {
        if (this.currentState !== GameState.STREET_CHASE) return;

        const cop = document.getElementById('cop-sprite');
        const currentLeft = parseFloat(cop.style.left) || 30;

        switch (dir) {
            case 'left':
                cop.style.left = `${Math.max(10, currentLeft - 15)}%`;
                break;
            case 'right':
                cop.style.left = `${Math.min(80, currentLeft + 15)}%`;
                break;
            case 'jump':
                cop.style.bottom = '40%';
                setTimeout(() => {
                    cop.style.bottom = '20%';
                }, 300);
                break;
        }
    }

    sprint() {
        if (this.stamina >= 20) {
            this.stamina -= 20;
            this.chaseDistance -= 15;
            this.updateChaseUI();
        }
    }

    winStreetChase() {
        cancelAnimationFrame(this.chaseAnimationId);
        document.getElementById('obstacles-container').innerHTML = '';

        // Transition to car chase
        this.addDialogue('system', "Suspect is getting into a vehicle! Get to your car!");

        setTimeout(() => {
            this.startCarChase();
        }, 2000);
    }

    // CAR CHASE
    startCarChase() {
        this.showScreen(GameState.CAR_CHASE);
        this.chaseDistance = 200;
        this.carHealth = 100;
        this.playerLane = 1;

        this.updateCarPosition();
        this.updateCarChaseUI();
        this.startTraffic();
        this.runCarChase();
    }

    runCarChase() {
        const gameLoop = () => {
            if (this.currentState !== GameState.CAR_CHASE) return;

            // Reduce distance
            this.chaseDistance -= 0.15;

            this.updateCarChaseUI();

            if (this.chaseDistance <= 0) {
                this.winGame();
                return;
            }

            if (this.carHealth <= 0) {
                this.loseGame();
                return;
            }

            this.chaseAnimationId = requestAnimationFrame(gameLoop);
        };

        this.chaseAnimationId = requestAnimationFrame(gameLoop);
    }

    startTraffic() {
        if (this.currentState !== GameState.CAR_CHASE) return;

        const container = document.getElementById('traffic-container');
        const cars = ['🚙', '🚕', '🚐', '🚚'];

        const car = document.createElement('div');
        car.className = 'traffic-car';
        car.textContent = cars[Math.floor(Math.random() * cars.length)];

        const lane = Math.floor(Math.random() * 3);
        car.style.left = `${20 + lane * 30}%`;
        car.dataset.lane = lane;

        container.appendChild(car);

        // Check collision
        const checkCollision = setInterval(() => {
            if (this.currentState !== GameState.CAR_CHASE) {
                clearInterval(checkCollision);
                return;
            }

            const carRect = car.getBoundingClientRect();
            const copRect = document.getElementById('cop-car').getBoundingClientRect();

            if (
                carRect.bottom > copRect.top &&
                carRect.top < copRect.bottom &&
                parseInt(car.dataset.lane) === this.playerLane
            ) {
                this.carHealth -= 25;
                document.getElementById('cop-car').textContent = '💥';
                setTimeout(() => {
                    document.getElementById('cop-car').textContent = '🚔';
                }, 300);
                clearInterval(checkCollision);
            }
        }, 100);

        car.addEventListener('animationend', () => {
            car.remove();
            clearInterval(checkCollision);
        });

        setTimeout(() => this.startTraffic(), 800 + Math.random() * 700);
    }

    changeLane(lane) {
        this.playerLane = lane;
        this.updateCarPosition();
    }

    updateCarPosition() {
        const cop = document.getElementById('cop-car');
        cop.style.left = `${20 + this.playerLane * 30}%`;
    }

    useNitro() {
        if (this.carHealth >= 10) {
            this.chaseDistance -= 30;
            this.carHealth -= 5;
            this.updateCarChaseUI();

            document.getElementById('cop-car').textContent = '🚔💨';
            setTimeout(() => {
                document.getElementById('cop-car').textContent = '🚔';
            }, 500);
        }
    }

    updateChaseUI() {
        document.getElementById('street-distance').textContent = Math.max(0, Math.round(this.chaseDistance));

        const staminaBars = Math.round(this.stamina / 12.5);
        document.getElementById('stamina-display').textContent = '█'.repeat(staminaBars) + '░'.repeat(8 - staminaBars);
    }

    updateCarChaseUI() {
        document.getElementById('car-distance').textContent = Math.max(0, Math.round(this.chaseDistance));

        const healthBars = Math.round(this.carHealth / 12.5);
        document.getElementById('car-health').textContent = '█'.repeat(healthBars) + '░'.repeat(8 - healthBars);
    }

    winGame() {
        cancelAnimationFrame(this.chaseAnimationId);
        document.getElementById('traffic-container').innerHTML = '';
        this.showResult(true, "Suspect apprehended! Excellent detective work!");
    }

    loseGame() {
        cancelAnimationFrame(this.chaseAnimationId);
        document.getElementById('traffic-container').innerHTML = '';
        this.showResult(false, "Your vehicle was too damaged. The suspect escaped!");
    }

    showResult(success, message) {
        this.showScreen(GameState.RESULT);

        const icon = document.getElementById('result-icon');
        const title = document.getElementById('result-title');
        const msg = document.getElementById('result-message');
        const stats = document.getElementById('result-stats');

        if (success) {
            icon.textContent = '🏆';
            title.textContent = 'MISSION COMPLETE';
            title.className = 'success';
        } else {
            icon.textContent = '❌';
            title.textContent = 'MISSION FAILED';
            title.className = 'failure';
        }

        msg.textContent = message;

        stats.innerHTML = `
            <p><strong>Suspect:</strong> ${this.suspectProfile.codename}</p>
            <p><strong>Customers Served:</strong> ${this.currentCustomerIndex + 1}</p>
            <p><strong>Final Reputation:</strong> ${'⭐'.repeat(this.reputation)}</p>
            <p><strong>Correct Identification:</strong> ${this.caughtCorrectSuspect ? '✅ Yes' : '❌ No'}</p>
        `;
    }

    resetGame() {
        this.currentState = GameState.TITLE;
        this.suspectProfile = null;
        this.customers = [];
        this.currentCustomerIndex = -1;
        this.currentCustomer = null;
        this.haircutProgress = 0;
        this.reputation = 3;
        this.trueSuspectIndex = -1;
        this.playerGuess = -1;
        this.caughtCorrectSuspect = false;
        this.chaseDistance = 100;
        this.stamina = 100;
        this.carHealth = 100;

        // Reset UI
        document.getElementById('next-customer-btn').classList.remove('hidden');
        document.getElementById('suspect-btn').classList.add('hidden');
        document.getElementById('haircut-area').classList.add('hidden');
        document.getElementById('dialogue-history').innerHTML = '';
        document.getElementById('obstacles-container').innerHTML = '';
        document.getElementById('traffic-container').innerHTML = '';

        this.showScreen(GameState.TITLE);
    }
}

// Initialize game
document.addEventListener('DOMContentLoaded', () => {
    window.game = new UndercoverBarberGame();
});
