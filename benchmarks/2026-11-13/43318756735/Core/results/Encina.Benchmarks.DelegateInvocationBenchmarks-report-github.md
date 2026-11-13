```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     37.47 ns |     2.857 ns |   0.157 ns |     1.25 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     29.97 ns |     6.411 ns |   0.351 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 89,097.43 ns | 4,062.387 ns | 222.673 ns | 2,972.89 |   30.74 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    135.89 ns |    50.294 ns |   2.757 ns |     4.53 |    0.09 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     84.25 ns |    17.514 ns |   0.960 ns |     2.81 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
