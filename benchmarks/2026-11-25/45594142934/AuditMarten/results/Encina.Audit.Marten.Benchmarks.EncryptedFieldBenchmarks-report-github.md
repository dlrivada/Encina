```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.86GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean            | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |----------------:|---------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   4,399.8380 ns |    232.9301 ns |    12.7677 ns |  1.249 |    0.01 |    2 |   0.0839 |        - |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 112,551.7494 ns | 28,719.3650 ns | 1,574.2053 ns | 31.941 |    0.51 |    4 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   3,966.5618 ns |    277.0934 ns |    15.1884 ns |  1.126 |    0.01 |    2 |   0.0153 |        - |        - |    1792 B |        1.20 |
| Decrypt_Long_4KB             |   8,033.1971 ns |    786.6434 ns |    43.1186 ns |  2.280 |    0.03 |    3 |   0.2594 |        - |        - |   22272 B |       14.89 |
| DecryptOrPlaceholder_NullKey |       0.3177 ns |      1.0979 ns |     0.0602 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   3,434.6224 ns |    824.7009 ns |    45.2046 ns |  0.975 |    0.02 |    2 |   0.0038 |        - |        - |     504 B |        0.34 |
| Decrypt_VeryLong_64KB        | 120,607.9839 ns | 20,631.3761 ns | 1,130.8753 ns | 34.227 |    0.45 |    4 |  68.8477 |  68.8477 |  68.8477 |  350037 B |      233.98 |
| Encrypt_Long_4KB             |   8,976.0771 ns |    898.9391 ns |    49.2739 ns |  2.547 |    0.03 |    3 |   0.4425 |   0.0153 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   3,524.1136 ns |    771.0700 ns |    42.2649 ns |  1.000 |    0.01 |    2 |   0.0153 |        - |        - |    1496 B |        1.00 |
