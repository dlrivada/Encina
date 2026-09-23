```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean           | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |---------------:|---------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |  3,462.7312 ns |  1,083.7740 ns |    59.4053 ns |  1.181 |    0.03 |    2 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 90,289.1509 ns | 16,962.8854 ns |   929.7930 ns | 30.794 |    0.68 |    5 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |  2,792.3731 ns |    577.9393 ns |    31.6788 ns |  0.952 |    0.02 |    2 |   0.0763 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |  5,157.3029 ns |    857.5748 ns |    47.0066 ns |  1.759 |    0.04 |    3 |   0.9918 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |      0.2806 ns |      0.5266 ns |     0.0289 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |  2,682.8142 ns |  4,679.6036 ns |   256.5049 ns |  0.915 |    0.08 |    2 |   0.0191 |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        | 74,242.3848 ns | 34,002.1647 ns | 1,863.7734 ns | 25.321 |    0.75 |    4 |  41.6260 |  41.6260 |  41.6260 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |  5,385.5752 ns |  6,418.7498 ns |   351.8333 ns |  1.837 |    0.11 |    3 |   2.2125 |   0.0839 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |  2,933.0968 ns |  1,253.7406 ns |    68.7218 ns |  1.000 |    0.03 |    2 |   0.0877 |        - |        - |    1496 B |        1.00 |
