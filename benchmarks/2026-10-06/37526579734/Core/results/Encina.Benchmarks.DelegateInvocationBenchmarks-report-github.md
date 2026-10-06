```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     37.29 ns |     3.099 ns |   0.170 ns |     1.22 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     30.49 ns |    12.552 ns |   0.688 ns |     1.00 |    0.03 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 91,964.43 ns | 3,093.626 ns | 169.572 ns | 3,017.22 |   59.93 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    136.20 ns |     4.743 ns |   0.260 ns |     4.47 |    0.09 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     99.88 ns |    19.259 ns |   1.056 ns |     3.28 |    0.07 | 0.0105 |      - |     176 B |        1.57 |
