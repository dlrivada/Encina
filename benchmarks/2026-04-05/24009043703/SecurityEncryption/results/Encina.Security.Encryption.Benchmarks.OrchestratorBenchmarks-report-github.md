```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                            | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean            | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |----------------:|---------:|---------:|------:|-------:|----------:|------------:|
| Encrypt_SingleProperty            | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      5,503.7 ns | 17.99 ns | 10.70 ns |  1.00 | 0.0610 |    1064 B |        1.00 |
| Encrypt_ThreeProperties           | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     15,949.6 ns | 29.02 ns | 19.20 ns |  2.90 | 0.1526 |    2912 B |        2.74 |
| EncryptDecrypt_Roundtrip          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     10,554.7 ns | 22.78 ns | 15.07 ns |  1.92 | 0.1221 |    2288 B |        2.15 |
| NoEncryptedProperties_Passthrough | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        123.0 ns |  0.34 ns |  0.20 ns |  0.02 | 0.0038 |      64 B |        0.06 |
|                                   |            |                |             |             |              |             |                 |          |          |       |        |           |             |
| Encrypt_SingleProperty            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 67,916,952.0 ns |       NA |  0.00 ns |  1.00 |      - |    2712 B |        1.00 |
| Encrypt_ThreeProperties           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 68,728,765.0 ns |       NA |  0.00 ns |  1.01 |      - |    7360 B |        2.71 |
| EncryptDecrypt_Roundtrip          | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 87,575,714.0 ns |       NA |  0.00 ns |  1.29 |      - |    2920 B |        1.08 |
| NoEncryptedProperties_Passthrough | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 22,939,548.0 ns |       NA |  0.00 ns |  0.34 |      - |      64 B |        0.02 |
