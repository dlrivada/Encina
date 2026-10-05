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
| CompiledDelegate        |     36.06 ns |     2.622 ns |   0.144 ns |     1.22 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     29.48 ns |     8.365 ns |   0.459 ns |     1.00 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 90,810.14 ns | 8,540.730 ns | 468.146 ns | 3,081.13 |   43.49 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    134.44 ns |    10.482 ns |   0.575 ns |     4.56 |    0.06 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     76.44 ns |    13.843 ns |   0.759 ns |     2.59 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
