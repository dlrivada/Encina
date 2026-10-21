```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     40.26 ns |    11.650 ns |   0.639 ns |     1.06 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     37.99 ns |    10.886 ns |   0.597 ns |     1.00 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 93,706.16 ns | 5,633.629 ns | 308.798 ns | 2,466.81 |   34.09 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    137.51 ns |     7.063 ns |   0.387 ns |     3.62 |    0.05 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     87.44 ns |     9.672 ns |   0.530 ns |     2.30 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
