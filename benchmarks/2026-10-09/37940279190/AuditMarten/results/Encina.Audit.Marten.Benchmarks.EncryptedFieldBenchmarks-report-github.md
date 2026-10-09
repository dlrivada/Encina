```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean           | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |---------------:|---------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   5,787.577 ns |  1,667.9086 ns |    91.4237 ns |  1.171 |    0.02 |    2 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 151,669.722 ns | 21,511.0776 ns | 1,179.0948 ns | 30.685 |    0.21 |    4 | 137.6953 | 137.6953 | 137.6953 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   4,926.632 ns |     51.0143 ns |     2.7963 ns |  0.997 |    0.00 |    2 |   0.0763 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |   9,270.688 ns |  3,935.2140 ns |   215.7024 ns |  1.876 |    0.04 |    3 |   0.9918 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |       1.064 ns |      0.1495 ns |     0.0082 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   4,388.751 ns |    244.0250 ns |    13.3758 ns |  0.888 |    0.00 |    2 |   0.0153 |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        | 137,270.790 ns |  7,637.8935 ns |   418.6587 ns | 27.772 |    0.08 |    4 |  41.5039 |  41.5039 |  41.5039 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |  10,660.825 ns |  2,162.4932 ns |   118.5335 ns |  2.157 |    0.02 |    3 |   2.2125 |   0.0763 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   4,942.783 ns |     60.9594 ns |     3.3414 ns |  1.000 |    0.00 |    2 |   0.0839 |        - |        - |    1496 B |        1.00 |
