```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.52GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                 | PeriodCount | Mean        | Error     | StdDev     | Median      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |------------:|----------:|-----------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |    87.98 ns |  1.520 ns |   2.180 ns |    88.16 ns |  1.16 |    0.04 |    3 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |    52.94 ns |  1.246 ns |   1.826 ns |    51.74 ns |  0.70 |    0.03 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 1,530.44 ns | 16.252 ns |  24.325 ns | 1,528.97 ns | 20.24 |    0.48 |    4 | 0.1469 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 3,094.33 ns | 90.351 ns | 129.578 ns | 3,114.87 ns | 40.92 |    1.84 |    5 | 0.0496 | 0.0458 |     848 B |        2.41 |
| GetExistingKey         | 12          |    75.65 ns |  0.936 ns |   1.400 ns |    75.60 ns |  1.00 |    0.03 |    2 | 0.0210 |      - |     352 B |        1.00 |
