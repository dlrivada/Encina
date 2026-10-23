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
| CompiledDelegate        |     39.27 ns |     2.700 ns |   0.148 ns |     1.21 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     32.56 ns |     6.228 ns |   0.341 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 92,081.72 ns | 2,355.509 ns | 129.113 ns | 2,828.16 |   26.04 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    142.28 ns |    12.607 ns |   0.691 ns |     4.37 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     86.29 ns |    11.200 ns |   0.614 ns |     2.65 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
