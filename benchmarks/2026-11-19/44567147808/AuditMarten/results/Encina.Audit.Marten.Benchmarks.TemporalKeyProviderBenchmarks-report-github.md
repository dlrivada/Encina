```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean        | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |------------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   130.30 ns |  20.869 ns |  1.144 ns |  1.07 |    0.02 |    2 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |    80.47 ns |   8.299 ns |  0.455 ns |  0.66 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,073.70 ns | 124.397 ns |  6.819 ns | 16.97 |    0.27 |    3 | 0.1450 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 3,700.97 ns | 540.731 ns | 29.639 ns | 30.29 |    0.52 |    4 | 0.0496 | 0.0458 |     848 B |        2.41 |
| GetExistingKey         | 12          |   122.22 ns |  40.335 ns |  2.211 ns |  1.00 |    0.02 |    2 | 0.0210 |      - |     352 B |        1.00 |
