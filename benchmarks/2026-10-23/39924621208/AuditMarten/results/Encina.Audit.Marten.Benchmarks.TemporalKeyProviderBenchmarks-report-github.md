```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean        | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |------------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   164.94 ns |  16.582 ns |  0.909 ns |  1.12 |    0.01 |    2 | 0.0129 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |    95.96 ns |   4.810 ns |  0.264 ns |  0.65 |    0.00 |    1 | 0.0044 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,931.46 ns | 133.898 ns |  7.339 ns | 19.87 |    0.12 |    3 | 0.0954 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 3,765.57 ns | 967.402 ns | 53.027 ns | 25.52 |    0.34 |    4 | 0.0305 | 0.0267 |     848 B |        2.41 |
| GetExistingKey         | 12          |   147.57 ns |  17.216 ns |  0.944 ns |  1.00 |    0.01 |    2 | 0.0138 |      - |     352 B |        1.00 |
