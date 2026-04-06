```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method                            | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean            | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |----------------:|---------:|---------:|------:|-------:|----------:|------------:|
| Encrypt_SingleProperty            | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      5,526.0 ns | 14.22 ns |  7.43 ns |  1.00 | 0.0381 |    1064 B |        1.00 |
| Encrypt_ThreeProperties           | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     15,982.9 ns | 37.22 ns | 24.62 ns |  2.89 | 0.0916 |    2912 B |        2.74 |
| EncryptDecrypt_Roundtrip          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     10,588.1 ns | 19.34 ns | 10.12 ns |  1.92 | 0.0763 |    2288 B |        2.15 |
| NoEncryptedProperties_Passthrough | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        108.8 ns |  0.29 ns |  0.17 ns |  0.02 | 0.0025 |      64 B |        0.06 |
|                                   |            |                |             |             |              |             |                 |          |          |       |        |           |             |
| Encrypt_SingleProperty            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 64,354,141.0 ns |       NA |  0.00 ns |  1.00 |      - |    2712 B |        1.00 |
| Encrypt_ThreeProperties           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 66,500,178.0 ns |       NA |  0.00 ns |  1.03 |      - |    7360 B |        2.71 |
| EncryptDecrypt_Roundtrip          | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 85,182,981.0 ns |       NA |  0.00 ns |  1.32 |      - |    2920 B |        1.08 |
| NoEncryptedProperties_Passthrough | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 21,415,127.0 ns |       NA |  0.00 ns |  0.33 |      - |      64 B |        0.02 |
