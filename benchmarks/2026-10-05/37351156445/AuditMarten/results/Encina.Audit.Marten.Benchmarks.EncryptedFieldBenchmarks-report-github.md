```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean           | Error         | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |---------------:|--------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   4,741.576 ns |    800.663 ns |    43.8870 ns |  1.222 |    0.01 |    3 |   0.0839 |        - |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 100,035.690 ns | 39,446.923 ns | 2,162.2190 ns | 25.787 |    0.51 |    6 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   3,879.548 ns |    281.016 ns |    15.4035 ns |  1.000 |    0.01 |    2 |   0.0153 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |   7,347.648 ns |  2,222.974 ns |   121.8487 ns |  1.894 |    0.03 |    4 |   0.1984 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |       1.104 ns |      1.715 ns |     0.0940 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   3,505.065 ns |  1,475.075 ns |    80.8538 ns |  0.904 |    0.02 |    2 |   0.0038 |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        |  83,121.397 ns |  3,132.910 ns |   171.7254 ns | 21.427 |    0.13 |    6 |  41.6260 |  41.6260 |  41.6260 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |   9,168.767 ns |  2,691.835 ns |   147.5486 ns |  2.363 |    0.04 |    5 |   0.4425 |   0.0153 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   3,879.446 ns |    476.124 ns |    26.0979 ns |  1.000 |    0.01 |    2 |   0.0153 |        - |        - |    1496 B |        1.00 |
