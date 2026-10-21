```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.82GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     28.78 ns |    54.347 ns |   2.979 ns |     1.21 |    0.11 | 0.0013 |      - |     112 B |        1.00 |
| DirectCall              |     23.74 ns |     9.371 ns |   0.514 ns |     1.00 |    0.03 | 0.0013 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 24,175.16 ns | 2,474.332 ns | 135.626 ns | 1,018.44 |   19.87 | 0.0610 | 0.0305 |    5455 B |       48.71 |
| GenericTypeConstruction |     93.70 ns |     5.931 ns |   0.325 ns |     3.95 |    0.08 | 0.0020 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     67.82 ns |    31.455 ns |   1.724 ns |     2.86 |    0.08 | 0.0020 |      - |     176 B |        1.57 |
