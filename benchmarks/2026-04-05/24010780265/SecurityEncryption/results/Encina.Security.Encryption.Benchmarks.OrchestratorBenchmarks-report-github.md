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
| Encrypt_SingleProperty            | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      5,577.9 ns | 19.85 ns | 11.81 ns |  1.00 | 0.0610 |    1064 B |        1.00 |
| Encrypt_ThreeProperties           | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     15,706.8 ns | 24.50 ns | 16.21 ns |  2.82 | 0.1526 |    2912 B |        2.74 |
| EncryptDecrypt_Roundtrip          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     10,973.0 ns | 31.00 ns | 18.45 ns |  1.97 | 0.1221 |    2288 B |        2.15 |
| NoEncryptedProperties_Passthrough | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        115.3 ns |  0.32 ns |  0.21 ns |  0.02 | 0.0038 |      64 B |        0.06 |
|                                   |            |                |             |             |              |             |                 |          |          |       |        |           |             |
| Encrypt_SingleProperty            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 67,966,423.0 ns |       NA |  0.00 ns |  1.00 |      - |    2712 B |        1.00 |
| Encrypt_ThreeProperties           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 70,713,316.0 ns |       NA |  0.00 ns |  1.04 |      - |    7360 B |        2.71 |
| EncryptDecrypt_Roundtrip          | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 88,505,087.0 ns |       NA |  0.00 ns |  1.30 |      - |    2920 B |        1.08 |
| NoEncryptedProperties_Passthrough | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 22,741,514.0 ns |       NA |  0.00 ns |  0.33 |      - |      64 B |        0.02 |
