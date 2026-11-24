```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.19GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error         | StdDev       | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|--------------:|-------------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     38.72 ns |      2.595 ns |     0.142 ns |     1.27 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     30.48 ns |     10.617 ns |     0.582 ns |     1.00 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 93,415.46 ns | 20,550.224 ns | 1,126.427 ns | 3,065.40 |   60.25 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    136.54 ns |     40.705 ns |     2.231 ns |     4.48 |    0.10 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     85.92 ns |     13.279 ns |     0.728 ns |     2.82 |    0.05 | 0.0105 |      - |     176 B |        1.57 |
