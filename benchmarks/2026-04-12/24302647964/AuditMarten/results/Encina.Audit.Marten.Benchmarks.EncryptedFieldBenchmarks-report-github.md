```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                       | Mean           | Error       | StdDev        | Median         | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |---------------:|------------:|--------------:|---------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   6,078.731 ns | 147.8638 ns |   212.0618 ns |   6,070.864 ns |  1.223 |    0.04 |    5 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 148,782.685 ns | 649.5249 ns |   931.5293 ns | 148,569.048 ns | 29.927 |    0.20 |    9 | 137.6953 | 137.6953 | 137.6953 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   4,659.555 ns |  23.9147 ns |    35.0538 ns |   4,660.738 ns |  0.937 |    0.01 |    3 |   0.0763 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |   8,861.797 ns |  88.2391 ns |   126.5499 ns |   8,851.343 ns |  1.783 |    0.03 |    6 |   0.9918 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |       1.059 ns |   0.0023 ns |     0.0030 ns |       1.059 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   4,131.988 ns |  30.2646 ns |    40.4024 ns |   4,164.153 ns |  0.831 |    0.01 |    2 |   0.0153 |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        | 128,940.498 ns | 810.8477 ns | 1,213.6387 ns | 128,914.225 ns | 25.936 |    0.25 |    8 |  41.5039 |  41.5039 |  41.5039 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |  10,198.029 ns |  78.5540 ns |   110.1217 ns |  10,197.920 ns |  2.051 |    0.02 |    7 |   2.2125 |   0.0763 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   4,971.488 ns |  10.3696 ns |    14.8718 ns |   4,973.883 ns |  1.000 |    0.00 |    4 |   0.0839 |        - |        - |    1496 B |        1.00 |
