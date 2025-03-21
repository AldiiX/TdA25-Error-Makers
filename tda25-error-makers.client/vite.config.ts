import { fileURLToPath, URL } from 'node:url';
import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-vue';
import fs from 'fs';
import path from 'path';
import child_process from 'child_process';
import { env } from 'process';

const target = env.ASPNETCORE_HTTPS_PORT
    ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}`
    : env.ASPNETCORE_URLS
        ? env.ASPNETCORE_URLS.split(';')[0]
        : 'https://localhost:7055';

export default defineConfig(({ command, mode }: any): any => {
    // Produkční konfigurace: vypínáme HTTPS (a tedy ani generování certifikátů)
    if (mode === 'production') {
        return {
            plugins: [plugin()],
            resolve: {
                alias: {
                    '@': fileURLToPath(new URL('./src', import.meta.url))
                }
            },
            server: {
                proxy: {
                    '^/api/': {
                        target,
                        secure: false
                    },
                    '^/ws/': {
                        target,
                        secure: false,
                        ws: true,
                    }
                },
                port: 8115,  // Port pro frontend
                host: '0.0.0.0', // Naslouchá na všech IP
                https: false
            },
            preview: {
                allowedHosts: ["*"]
            }
        };
    }

    // Vývojová konfigurace: generace certifikátů a nastavení HTTPS
    /*const baseFolder =
        env.APPDATA !== undefined && env.APPDATA !== ''
            ? `${env.APPDATA}/ASP.NET/https`
            : `${env.HOME}/.aspnet/https`;

    const certificateName = "stanislavskudrna.cz.client";
    const certFilePath = path.join(baseFolder, `${certificateName}.pem`);
    const keyFilePath = path.join(baseFolder, `${certificateName}.key`);

    if (!fs.existsSync(baseFolder)) {
        fs.mkdirSync(baseFolder, { recursive: true });
    }

    if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
        const result = child_process.spawnSync('dotnet', [
            'dev-certs',
            'https',
            '--export-path',
            certFilePath,
            '--format',
            'Pem',
            '--no-password',
        ], { stdio: 'inherit' });
        if (result.status !== 0) {
            throw new Error("Could not create certificate.");
        }
    }*/

    return {
        plugins: [plugin()],
        resolve: {
            alias: {
                '@': fileURLToPath(new URL('./src', import.meta.url))
            }
        },
        server: {
            proxy: {
                '^/api/': {
                    target,
                    secure: false
                },
                '^/ws/': {
                    target,
                    secure: false,
                    ws:true,
                },
                '^/openapi/': {
                    target,
                    secure: false
                }
            },
            port: 8115, // Port pro frontend
            host: '0.0.0.0', // Naslouchá na všech IP
            https: false, /*{
                key: fs.readFileSync(keyFilePath),
                cert: fs.readFileSync(certFilePath),
            }*/
        }
    };
});
