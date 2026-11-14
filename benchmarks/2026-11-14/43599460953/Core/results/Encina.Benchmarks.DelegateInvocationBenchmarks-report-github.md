```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error         | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|--------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     27.25 ns |      4.508 ns |   0.247 ns |     1.27 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     21.40 ns |      0.678 ns |   0.037 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 52,561.54 ns | 10,375.607 ns | 568.722 ns | 2,456.50 |   23.31 | 0.3052 | 0.2441 |    5287 B |       47.21 |
| GenericTypeConstruction |    101.19 ns |      6.236 ns |   0.342 ns |     4.73 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     65.04 ns |     12.291 ns |   0.674 ns |     3.04 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
