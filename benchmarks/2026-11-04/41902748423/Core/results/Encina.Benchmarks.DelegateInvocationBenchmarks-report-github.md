```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     37.60 ns |     4.454 ns |   0.244 ns |     1.24 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     30.25 ns |     7.348 ns |   0.403 ns |     1.00 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 93,362.20 ns | 4,750.051 ns | 260.366 ns | 3,086.25 |   36.52 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    134.77 ns |    24.263 ns |   1.330 ns |     4.45 |    0.06 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     83.20 ns |    12.667 ns |   0.694 ns |     2.75 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
