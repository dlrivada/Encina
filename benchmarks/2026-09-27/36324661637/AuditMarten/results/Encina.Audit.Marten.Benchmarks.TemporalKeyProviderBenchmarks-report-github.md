```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean        | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |------------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |    80.27 ns |   0.781 ns |  0.043 ns |  1.09 |    0.00 |    2 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |    55.14 ns |   1.614 ns |  0.088 ns |  0.75 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 1,456.19 ns | 202.032 ns | 11.074 ns | 19.73 |    0.16 |    3 | 0.1469 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 2,705.39 ns | 843.537 ns | 46.237 ns | 36.65 |    0.57 |    4 | 0.0496 | 0.0458 |     848 B |        2.41 |
| GetExistingKey         | 12          |    73.82 ns |   7.046 ns |  0.386 ns |  1.00 |    0.01 |    2 | 0.0210 |      - |     352 B |        1.00 |
