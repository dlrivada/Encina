```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     39.03 ns |     9.659 ns |   0.529 ns |     1.25 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     31.18 ns |     5.913 ns |   0.324 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 93,108.32 ns | 5,577.772 ns | 305.736 ns | 2,986.53 |   28.17 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    133.19 ns |    17.377 ns |   0.952 ns |     4.27 |    0.05 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     84.55 ns |     7.412 ns |   0.406 ns |     2.71 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
