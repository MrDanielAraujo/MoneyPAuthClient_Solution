using System.Security.Cryptography;

namespace MoneyPAuthClient.Utils;

/// <summary>
/// Utilitário para geração de par de chaves RSA
/// </summary>
public static class KeyGenerator
{
    /// <summary>
    /// Gera um par de chaves RSA de 2048 bits
    /// </summary>
    /// <param name="privateKeyPath">Caminho para salvar a chave privada</param>
    /// <param name="publicKeyPath">Caminho para salvar a chave pública</param>
    public static void GenerateKeyPair(string privateKeyPath = "private.pem", string publicKeyPath = "public.pem")
    {
        using var rsa = RSA.Create(2048);

        // Exporta a chave privada no formato PEM
        var privateKey = rsa.ExportRSAPrivateKeyPem();
        File.WriteAllText(privateKeyPath, privateKey);

        // Exporta a chave pública no formato PEM
        var publicKey = rsa.ExportSubjectPublicKeyInfoPem();
        File.WriteAllText(publicKeyPath, publicKey);

        Console.WriteLine($"Chave privada salva em: {Path.GetFullPath(privateKeyPath)}");
        Console.WriteLine($"Chave pública salva em: {Path.GetFullPath(publicKeyPath)}");
        Console.WriteLine("\nIMPORTANTE: Guarde a chave privada em local seguro e nunca a compartilhe!");
        Console.WriteLine("Envie APENAS a chave pública para a equipe MoneyP.");
    }

    /// <summary>
    /// Valida se um arquivo PEM contém uma chave privada RSA válida
    /// </summary>
    /// <param name="privateKeyPath">Caminho do arquivo de chave privada</param>
    /// <returns>True se a chave é válida</returns>
    public static bool ValidatePrivateKey(string privateKeyPath)
    {
        try
        {
            var privateKeyPem = File.ReadAllText(privateKeyPath);
            using var rsa = RSA.Create();
            rsa.ImportFromPem(privateKeyPem);
            
            // Testa se consegue assinar dados
            var testData = new byte[] { 1, 2, 3, 4, 5 };
            var signature = rsa.SignData(testData, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            
            return signature.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Obtém informações sobre uma chave RSA
    /// </summary>
    /// <param name="keyPath">Caminho do arquivo de chave</param>
    /// <returns>Informações da chave</returns>
    public static string GetKeyInfo(string keyPath)
    {
        try
        {
            var keyPem = File.ReadAllText(keyPath);
            using var rsa = RSA.Create();
            
            if (keyPem.Contains("PRIVATE KEY"))
            {
                rsa.ImportFromPem(keyPem);
                return $"Chave Privada RSA - {rsa.KeySize} bits";
            }
            else if (keyPem.Contains("PUBLIC KEY"))
            {
                rsa.ImportFromPem(keyPem);
                return $"Chave Pública RSA - {rsa.KeySize} bits";
            }
            
            return "Tipo de chave desconhecido";
        }
        catch (Exception ex)
        {
            return $"Erro ao ler chave: {ex.Message}";
        }
    }
}
