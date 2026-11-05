```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.93GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean            | Error         | StdDev      | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |----------------:|--------------:|------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   4,545.9285 ns |   518.0437 ns |  28.3957 ns |  1.269 |    0.01 |    2 |   0.0839 |        - |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 118,404.3057 ns | 6,567.4113 ns | 359.9820 ns | 33.060 |    0.09 |    4 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   3,994.4901 ns |    45.8355 ns |   2.5124 ns |  1.115 |    0.00 |    2 |   0.0153 |        - |        - |    1792 B |        1.20 |
| Decrypt_Long_4KB             |   8,200.9415 ns |   257.9779 ns |  14.1406 ns |  2.290 |    0.00 |    3 |   0.2594 |        - |        - |   22272 B |       14.89 |
| DecryptOrPlaceholder_NullKey |       0.2626 ns |     0.1328 ns |   0.0073 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   3,475.3427 ns |    94.9780 ns |   5.2061 ns |  0.970 |    0.00 |    2 |   0.0038 |        - |        - |     504 B |        0.34 |
| Decrypt_VeryLong_64KB        | 123,629.8807 ns | 5,084.9293 ns | 278.7221 ns | 34.520 |    0.08 |    4 |  68.8477 |  68.8477 |  68.8477 |  350037 B |      233.98 |
| Encrypt_Long_4KB             |   9,399.0356 ns | 1,201.5931 ns |  65.8634 ns |  2.624 |    0.02 |    3 |   0.4425 |   0.0153 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   3,581.4470 ns |    81.8116 ns |   4.4844 ns |  1.000 |    0.00 |    2 |   0.0153 |        - |        - |    1496 B |        1.00 |
