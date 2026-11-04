```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean       | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |-----------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   183.2 ns |    16.52 ns |  0.91 ns |  1.25 |    0.01 |    3 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |   110.7 ns |    33.89 ns |  1.86 ns |  0.75 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,749.5 ns |   261.74 ns | 14.35 ns | 18.73 |    0.22 |    4 | 0.1450 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 3,932.9 ns | 1,306.48 ns | 71.61 ns | 26.79 |    0.51 |    5 | 0.0496 | 0.0458 |     848 B |        2.41 |
| GetExistingKey         | 12          |   146.8 ns |    33.59 ns |  1.84 ns |  1.00 |    0.02 |    2 | 0.0210 |      - |     352 B |        1.00 |
