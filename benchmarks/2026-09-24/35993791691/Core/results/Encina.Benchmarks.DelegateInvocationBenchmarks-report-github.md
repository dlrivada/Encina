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
| CompiledDelegate        |     35.70 ns |     0.727 ns |   0.040 ns |     1.24 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     28.70 ns |    10.418 ns |   0.571 ns |     1.00 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 91,234.37 ns | 5,368.853 ns | 294.285 ns | 3,180.23 |   54.92 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    134.90 ns |    34.453 ns |   1.888 ns |     4.70 |    0.10 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     75.40 ns |     2.462 ns |   0.135 ns |     2.63 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
