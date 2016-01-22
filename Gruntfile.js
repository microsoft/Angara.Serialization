/// <binding />
module.exports = function (grunt) {

    grunt.initConfig({
        dirs: {
            project: "src/Angara.SerializationJS",
            src: "<%=dirs.project%>/src" 
        },
        bower: {
            install: {
                options: {
                    targetDir: './lib',
                    //layout: 'byType',
                    install: true,
                    verbose: false,
                    cleanTargetDir: false,
                    cleanBowerDir: false,
                    bowerOptions: {}
                }
            }
        },
        concat: {
            "umd": {
                dest: "dist/Angara.Serialization.umd.js",
                src: [
                    "<%=dirs.src%>/header.txt",
                    "<%=dirs.src%>/Angara.InfoSet.js",
                    "<%=dirs.src%>/footer.txt"
                ]
            },
            "umdTs": {
                dest: "dist/Angara.Serialization.umd.d.ts",
                src: [ "src/Angara.SerializationJS/src/Angara.InfoSet.d.ts"],
                options: {
                    footer: "export = Angara;"
                }
            }
        },
        jasmine: {
            options: {
                keepRunner: true,
                vendor: [ "dist/Angara.Serialization.js" ]
            },
            src: ['src/Angara.SerializationJS/test/**/*.js']
        },
        copy: {
            main: {
                files: [
                    { src: "<%=dirs.src%>/Angara.InfoSet.d.ts", dest: "dist/Angara.Serialization.d.ts"},
                    { src: "<%=dirs.src%>/Angara.InfoSet.js", dest: "dist/Angara.Serialization.js"},
                ]
            }
        },
        ts: {
            "infoSet": {
                options: {
                    target: 'es5',
                    sourceMap: false,
                    declaration: true
                },
                src: ["src/Angara.SerializationJS/src/Angara.InfoSet.ts"]
            },
            "test": {
                options: {
                    target: 'es5',
                    sourceMap: false,
                },
                src: ["src/Angara.SerializationJS/test/*.ts"]
            }
        },
        tsd: {
            refresh: {
                options: {
                    // execute a command
                    command: 'reinstall',

                    //optional: always get from HEAD
                    latest: true,

                    // specify config file
                    config: 'tsd.json',

                    // experimental: options to pass to tsd.API
                    opts: {
                        // props from tsd.Options
                    }
                }
            }
        }
    });

    grunt.loadNpmTasks('grunt-contrib-concat');
    grunt.loadNpmTasks('grunt-contrib-copy');
    grunt.loadNpmTasks('grunt-contrib-jasmine');
    grunt.loadNpmTasks('grunt-bower-task');
    grunt.loadNpmTasks("grunt-ts");
    grunt.loadNpmTasks('grunt-tsd');
    
    grunt.registerTask('build', ['ts:infoSet', 'concat', 'copy', 'ts:test', 'jasmine' ]);
    grunt.registerTask('default', ['bower', 'tsd', 'build']);
};