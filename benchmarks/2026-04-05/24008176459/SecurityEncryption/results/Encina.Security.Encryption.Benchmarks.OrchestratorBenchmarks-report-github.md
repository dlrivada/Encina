```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                            | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean            | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |----------------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| Encrypt_SingleProperty            | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      5,599.0 ns | 43.81 ns | 28.98 ns |  1.00 |    0.01 | 0.0610 |    1064 B |        1.00 |
| Encrypt_ThreeProperties           | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     15,879.1 ns | 69.54 ns | 45.99 ns |  2.84 |    0.02 | 0.1526 |    2912 B |        2.74 |
| EncryptDecrypt_Roundtrip          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     10,407.3 ns | 51.70 ns | 34.20 ns |  1.86 |    0.01 | 0.1221 |    2288 B |        2.15 |
| NoEncryptedProperties_Passthrough | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        114.6 ns |  0.80 ns |  0.53 ns |  0.02 |    0.00 | 0.0038 |      64 B |        0.06 |
|                                   |            |                |             |             |              |             |                 |          |          |       |         |        |           |             |
| Encrypt_SingleProperty            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 70,845,407.0 ns |       NA |  0.00 ns |  1.00 |    0.00 |      - |    2712 B |        1.00 |
| Encrypt_ThreeProperties           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 71,153,635.0 ns |       NA |  0.00 ns |  1.00 |    0.00 |      - |    7360 B |        2.71 |
| EncryptDecrypt_Roundtrip          | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 91,599,615.0 ns |       NA |  0.00 ns |  1.29 |    0.00 |      - |    2920 B |        1.08 |
| NoEncryptedProperties_Passthrough | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 24,110,545.0 ns |       NA |  0.00 ns |  0.34 |    0.00 |      - |      64 B |        0.02 |
