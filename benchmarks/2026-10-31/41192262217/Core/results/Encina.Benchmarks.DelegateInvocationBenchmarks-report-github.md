```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     39.02 ns |     6.510 ns |   0.357 ns |     1.23 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     31.79 ns |     9.500 ns |   0.521 ns |     1.00 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 92,028.16 ns | 6,754.766 ns | 370.252 ns | 2,895.15 |   42.14 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    132.46 ns |     6.887 ns |   0.377 ns |     4.17 |    0.06 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     84.80 ns |     4.772 ns |   0.262 ns |     2.67 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
