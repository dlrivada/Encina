```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean            | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |----------------:|---------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   4,724.2039 ns |  1,087.1112 ns |    59.5882 ns |  1.250 |    0.01 |    3 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 105,348.1795 ns |  9,745.9609 ns |   534.2090 ns | 27.884 |    0.14 |    5 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   3,516.5041 ns |    444.3666 ns |    24.3572 ns |  0.931 |    0.01 |    2 |   0.0763 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |   6,111.3131 ns |  1,433.7987 ns |    78.5913 ns |  1.618 |    0.02 |    4 |   0.9918 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |       0.5575 ns |      0.1617 ns |     0.0089 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   3,186.3192 ns |    107.7833 ns |     5.9080 ns |  0.843 |    0.00 |    2 |   0.0191 |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        |  91,216.2975 ns | 19,336.9663 ns | 1,059.9244 ns | 24.143 |    0.25 |    5 |  41.6260 |  41.6260 |  41.6260 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |   7,040.2068 ns |  1,331.4432 ns |    72.9809 ns |  1.863 |    0.02 |    4 |   2.2125 |   0.0763 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   3,778.1369 ns |    181.7504 ns |     9.9624 ns |  1.000 |    0.00 |    2 |   0.0877 |        - |        - |    1496 B |        1.00 |
