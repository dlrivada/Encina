```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.01GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean            | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |----------------:|---------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   2,848.9625 ns |  1,439.3228 ns |    78.8941 ns |  1.126 |    0.03 |    2 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        |  86,423.2008 ns | 51,267.6320 ns | 2,810.1519 ns | 34.157 |    1.10 |    5 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   2,599.6626 ns |    389.4172 ns |    21.3453 ns |  1.027 |    0.02 |    2 |   0.1068 |        - |        - |    1792 B |        1.20 |
| Decrypt_Long_4KB             |   4,133.6267 ns |  1,431.1309 ns |    78.4451 ns |  1.634 |    0.04 |    3 |   1.3275 |   0.0076 |        - |   22272 B |       14.89 |
| DecryptOrPlaceholder_NullKey |       0.2105 ns |      0.1204 ns |     0.0066 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   2,257.2638 ns |    110.9430 ns |     6.0812 ns |  0.892 |    0.01 |    2 |   0.0267 |        - |        - |     504 B |        0.34 |
| Decrypt_VeryLong_64KB        | 111,834.8013 ns | 85,633.8297 ns | 4,693.8792 ns | 44.200 |    1.74 |    6 |  68.8477 |  68.8477 |  68.8477 |  350037 B |      233.98 |
| Encrypt_Long_4KB             |   5,000.8779 ns |  4,391.4864 ns |   240.7122 ns |  1.976 |    0.09 |    4 |   2.2125 |   0.0839 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   2,530.7002 ns |    810.8519 ns |    44.4455 ns |  1.000 |    0.02 |    2 |   0.0877 |        - |        - |    1496 B |        1.00 |
