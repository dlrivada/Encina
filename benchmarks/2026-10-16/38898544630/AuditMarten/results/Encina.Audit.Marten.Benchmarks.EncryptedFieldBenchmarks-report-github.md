```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean           | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |---------------:|---------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |  3,384.0749 ns |    978.1996 ns |    53.6184 ns |  1.155 |    0.02 |    2 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 81,124.4600 ns | 13,059.3675 ns |   715.8280 ns | 27.678 |    0.43 |    5 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |  2,767.8654 ns |  1,518.4193 ns |    83.2297 ns |  0.944 |    0.03 |    2 |   0.0763 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |  4,568.2637 ns |    813.7738 ns |    44.6057 ns |  1.559 |    0.02 |    3 |   0.9918 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |      0.5859 ns |      2.9761 ns |     0.1631 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |  2,524.2955 ns |    325.8657 ns |    17.8618 ns |  0.861 |    0.01 |    2 |   0.0191 |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        | 73,463.7881 ns | 31,749.3604 ns | 1,740.2896 ns | 25.065 |    0.62 |    5 |  41.6260 |  41.6260 |  41.6260 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |  5,790.1219 ns |  2,291.0661 ns |   125.5811 ns |  1.975 |    0.05 |    4 |   2.2125 |   0.0839 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |  2,931.4539 ns |    843.8738 ns |    46.2556 ns |  1.000 |    0.02 |    2 |   0.0877 |        - |        - |    1496 B |        1.00 |
