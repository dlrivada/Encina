```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.98GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean           | Error          | StdDev      | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |---------------:|---------------:|------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |  2,879.9782 ns |    353.6149 ns |  19.3828 ns |  1.177 |    0.01 |    2 |   0.0877 |        - |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 89,588.4612 ns | 11,728.7975 ns | 642.8950 ns | 36.607 |    0.23 |    4 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |  2,663.3361 ns |     57.5937 ns |   3.1569 ns |  1.088 |    0.00 |    2 |   0.0191 |        - |        - |    1792 B |        1.20 |
| Decrypt_Long_4KB             |  5,036.7072 ns |    636.1965 ns |  34.8721 ns |  2.058 |    0.01 |    3 |   0.2594 |        - |        - |   22272 B |       14.89 |
| DecryptOrPlaceholder_NullKey |      0.5096 ns |      0.0370 ns |   0.0020 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |  2,304.2326 ns |     61.4614 ns |   3.3689 ns |  0.942 |    0.00 |    2 |   0.0038 |        - |        - |     504 B |        0.34 |
| Decrypt_VeryLong_64KB        | 91,715.1844 ns |  9,298.9697 ns | 509.7079 ns | 37.476 |    0.18 |    4 |  68.8477 |  68.8477 |  68.8477 |  350037 B |      233.98 |
| Encrypt_Long_4KB             |  5,495.5479 ns |    325.7659 ns |  17.8563 ns |  2.246 |    0.01 |    3 |   0.4425 |   0.0153 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |  2,447.3351 ns |     22.7435 ns |   1.2466 ns |  1.000 |    0.00 |    2 |   0.0153 |        - |        - |    1496 B |        1.00 |
