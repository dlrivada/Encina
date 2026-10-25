```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     22.05 ns |     8.131 ns |   0.446 ns |     1.31 |    0.06 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     16.91 ns |    15.683 ns |   0.860 ns |     1.00 |    0.06 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 17,481.58 ns | 2,003.141 ns | 109.799 ns | 1,035.75 |   44.67 | 0.3052 | 0.2747 |    5287 B |       47.21 |
| GenericTypeConstruction |     74.37 ns |    13.862 ns |   0.760 ns |     4.41 |    0.19 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     48.90 ns |     8.605 ns |   0.472 ns |     2.90 |    0.13 | 0.0105 |      - |     176 B |        1.57 |
