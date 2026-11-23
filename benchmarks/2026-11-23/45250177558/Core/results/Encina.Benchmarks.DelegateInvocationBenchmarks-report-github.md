```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.19GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     36.82 ns |     4.599 ns |   0.252 ns |     1.31 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     28.21 ns |     2.119 ns |   0.116 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 90,112.77 ns | 5,210.864 ns | 285.625 ns | 3,194.17 |   14.37 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    130.75 ns |    17.487 ns |   0.959 ns |     4.63 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     82.84 ns |    24.478 ns |   1.342 ns |     2.94 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
