```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 3.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     21.33 ns |     0.985 ns |   0.054 ns |     1.33 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     16.09 ns |     2.726 ns |   0.149 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 17,387.17 ns | 5,370.474 ns | 294.374 ns | 1,080.64 |   18.05 | 0.3052 | 0.2747 |    5287 B |       47.21 |
| GenericTypeConstruction |     71.06 ns |    83.132 ns |   4.557 ns |     4.42 |    0.25 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     49.45 ns |     7.409 ns |   0.406 ns |     3.07 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
