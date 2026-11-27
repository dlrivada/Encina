```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.27GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     20.82 ns |     0.640 ns |   0.035 ns |     1.33 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     15.63 ns |     2.227 ns |   0.122 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 17,312.32 ns | 2,413.980 ns | 132.318 ns | 1,107.83 |   10.46 | 0.3052 | 0.2747 |    5287 B |       47.21 |
| GenericTypeConstruction |     69.73 ns |    21.281 ns |   1.166 ns |     4.46 |    0.07 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     45.53 ns |    37.602 ns |   2.061 ns |     2.91 |    0.12 | 0.0105 |      - |     176 B |        1.57 |
