```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error         | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|--------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     37.41 ns |     22.970 ns |   1.259 ns |     1.30 |    0.04 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     28.71 ns |      4.885 ns |   0.268 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 92,640.12 ns | 13,101.786 ns | 718.153 ns | 3,226.86 |   33.90 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    135.49 ns |      9.642 ns |   0.528 ns |     4.72 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     81.30 ns |      6.643 ns |   0.364 ns |     2.83 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
