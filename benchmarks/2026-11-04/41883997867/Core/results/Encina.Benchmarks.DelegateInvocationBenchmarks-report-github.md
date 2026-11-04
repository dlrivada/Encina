```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     38.79 ns |     6.416 ns |   0.352 ns |     1.25 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     31.15 ns |     4.806 ns |   0.263 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 90,886.71 ns | 4,820.616 ns | 264.234 ns | 2,917.59 |   22.63 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    136.11 ns |     8.864 ns |   0.486 ns |     4.37 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     80.66 ns |    14.281 ns |   0.783 ns |     2.59 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
