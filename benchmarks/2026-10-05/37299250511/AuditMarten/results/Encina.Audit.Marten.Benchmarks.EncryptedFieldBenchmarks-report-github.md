```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.37GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean           | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |---------------:|---------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   6,189.427 ns |    135.3060 ns |     7.4166 ns |  1.188 |    0.00 |    2 |   0.2975 |        - |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 120,908.739 ns | 59,410.1564 ns | 3,256.4712 ns | 23.216 |    0.54 |    5 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   5,127.193 ns |    214.7584 ns |    11.7716 ns |  0.985 |    0.00 |    2 |   0.0458 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |   9,193.595 ns |    976.0116 ns |    53.4985 ns |  1.765 |    0.01 |    3 |   0.6561 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |       1.341 ns |      0.1282 ns |     0.0070 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   4,607.531 ns |    401.6661 ns |    22.0167 ns |  0.885 |    0.00 |    2 |   0.0076 |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        | 111,513.792 ns | 12,767.2748 ns |   699.8174 ns | 21.413 |    0.13 |    5 |  41.6260 |  41.6260 |  41.6260 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |  11,180.140 ns |  2,229.5976 ns |   122.2118 ns |  2.147 |    0.02 |    4 |   1.4648 |   0.0763 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   5,207.905 ns |    255.1850 ns |    13.9876 ns |  1.000 |    0.00 |    2 |   0.0534 |        - |        - |    1496 B |        1.00 |
