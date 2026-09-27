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
| CompiledDelegate        |     38.66 ns |    25.554 ns |   1.401 ns |     1.29 |    0.05 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     29.91 ns |    10.942 ns |   0.600 ns |     1.00 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 91,495.07 ns | 5,256.924 ns | 288.150 ns | 3,059.95 |   53.57 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    137.08 ns |     0.611 ns |   0.033 ns |     4.58 |    0.08 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     75.69 ns |     7.080 ns |   0.388 ns |     2.53 |    0.05 | 0.0105 |      - |     176 B |        1.57 |
