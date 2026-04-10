```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     37.87 ns |    14.912 ns |   0.817 ns |     0.97 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     38.88 ns |     3.860 ns |   0.212 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 92,738.92 ns | 6,761.664 ns | 370.630 ns | 2,385.61 |   13.93 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    137.95 ns |     6.306 ns |   0.346 ns |     3.55 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     82.91 ns |    13.241 ns |   0.726 ns |     2.13 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
