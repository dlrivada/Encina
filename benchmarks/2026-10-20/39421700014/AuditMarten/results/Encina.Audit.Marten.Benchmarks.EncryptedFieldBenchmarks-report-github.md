```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.17GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean           | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |---------------:|---------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   4,988.217 ns |    319.0289 ns |    17.4870 ns |  1.200 |    0.04 |    3 |   0.0839 |        - |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 110,722.881 ns | 28,146.8963 ns | 1,542.8264 ns | 26.644 |    0.86 |    7 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   4,105.194 ns |    477.4308 ns |    26.1696 ns |  0.988 |    0.03 |    2 |   0.0153 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |   7,505.880 ns |    552.4488 ns |    30.2816 ns |  1.806 |    0.05 |    4 |   0.1984 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |       1.134 ns |      0.5602 ns |     0.0307 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   3,727.662 ns |    116.6602 ns |     6.3945 ns |  0.897 |    0.03 |    2 |        - |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        |  90,072.693 ns | 20,853.0934 ns | 1,143.0284 ns | 21.675 |    0.69 |    6 |  41.6260 |  41.6260 |  41.6260 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |   9,497.316 ns |  2,530.1158 ns |   138.6842 ns |  2.285 |    0.07 |    5 |   0.4425 |   0.0153 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   4,159.066 ns |  2,689.2348 ns |   147.4060 ns |  1.001 |    0.04 |    2 |   0.0153 |        - |        - |    1496 B |        1.00 |
