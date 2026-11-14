```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.32GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean            | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |----------------:|---------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   3,081.3062 ns |    821.4847 ns |    45.0283 ns |  1.217 |    0.03 |    3 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        |  79,156.3893 ns |  1,702.2461 ns |    93.3059 ns | 31.272 |    0.54 |    6 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   2,402.2941 ns |    165.1191 ns |     9.0507 ns |  0.949 |    0.02 |    2 |   0.1068 |        - |        - |    1792 B |        1.20 |
| Decrypt_Long_4KB             |   4,003.4824 ns |  2,227.2003 ns |   122.0804 ns |  1.582 |    0.05 |    4 |   1.3275 |   0.0076 |        - |   22272 B |       14.89 |
| DecryptOrPlaceholder_NullKey |       0.2480 ns |      0.6221 ns |     0.0341 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   2,124.0286 ns |  1,007.2467 ns |    55.2106 ns |  0.839 |    0.02 |    2 |   0.0267 |        - |        - |     504 B |        0.34 |
| Decrypt_VeryLong_64KB        | 107,032.0551 ns | 46,935.7286 ns | 2,572.7057 ns | 42.284 |    1.14 |    7 |  68.8477 |  68.8477 |  68.8477 |  350037 B |      233.98 |
| Encrypt_Long_4KB             |   4,828.9180 ns |  3,805.8704 ns |   208.6126 ns |  1.908 |    0.08 |    5 |   2.2125 |   0.0839 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   2,531.9239 ns |    921.8832 ns |    50.5315 ns |  1.000 |    0.02 |    2 |   0.0877 |        - |        - |    1496 B |        1.00 |
