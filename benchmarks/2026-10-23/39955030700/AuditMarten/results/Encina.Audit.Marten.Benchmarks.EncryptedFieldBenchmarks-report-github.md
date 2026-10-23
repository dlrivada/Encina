```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean           | Error         | StdDev      | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |---------------:|--------------:|------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   5,173.988 ns |    654.676 ns |  35.8850 ns |  1.146 |    0.01 |    2 |   0.0839 |        - |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 115,129.107 ns | 15,682.894 ns | 859.6323 ns | 25.500 |    0.17 |    6 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   4,397.421 ns |  1,116.441 ns |  61.1959 ns |  0.974 |    0.01 |    2 |   0.0153 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |   8,123.099 ns |  1,742.982 ns |  95.5387 ns |  1.799 |    0.02 |    3 |   0.1984 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |       1.267 ns |      1.993 ns |   0.1092 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   3,799.151 ns |    813.079 ns |  44.5676 ns |  0.841 |    0.01 |    2 |        - |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        |  90,066.350 ns |  5,198.576 ns | 284.9515 ns | 19.949 |    0.07 |    5 |  41.6260 |  41.6260 |  41.6260 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |   9,806.892 ns |  1,226.039 ns |  67.2033 ns |  2.172 |    0.01 |    4 |   0.4425 |   0.0153 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   4,514.810 ns |    188.784 ns |  10.3479 ns |  1.000 |    0.00 |    2 |   0.0153 |        - |        - |    1496 B |        1.00 |
