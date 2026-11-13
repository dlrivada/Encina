```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean            | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |----------------:|---------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   4,213.1020 ns |  1,519.7700 ns |    83.3037 ns |  1.212 |    0.02 |    3 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 107,498.7624 ns | 46,255.1633 ns | 2,535.4016 ns | 30.926 |    0.66 |    5 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   3,203.4259 ns |    634.8809 ns |    34.8000 ns |  0.922 |    0.01 |    2 |   0.1068 |        - |        - |    1792 B |        1.20 |
| Decrypt_Long_4KB             |   6,055.8806 ns |  2,637.3688 ns |   144.5631 ns |  1.742 |    0.04 |    4 |   1.3275 |   0.0076 |        - |   22272 B |       14.89 |
| DecryptOrPlaceholder_NullKey |       0.5343 ns |      4.9715 ns |     0.2725 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   2,834.0692 ns |    186.7849 ns |    10.2383 ns |  0.815 |    0.01 |    2 |   0.0267 |        - |        - |     504 B |        0.34 |
| Decrypt_VeryLong_64KB        | 126,413.5479 ns |  9,208.1876 ns |   504.7318 ns | 36.368 |    0.25 |    5 |  68.8477 |  68.8477 |  68.8477 |  350037 B |      233.98 |
| Encrypt_Long_4KB             |   6,676.3810 ns |  2,554.7062 ns |   140.0321 ns |  1.921 |    0.04 |    4 |   2.2125 |   0.0839 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   3,476.0798 ns |    427.0315 ns |    23.4070 ns |  1.000 |    0.01 |    2 |   0.0877 |        - |        - |    1496 B |        1.00 |
