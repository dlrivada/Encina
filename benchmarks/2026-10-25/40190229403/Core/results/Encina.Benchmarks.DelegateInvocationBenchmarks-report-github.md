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
| CompiledDelegate        |     37.77 ns |    11.014 ns |   0.604 ns |     1.28 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     29.44 ns |     1.424 ns |   0.078 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 89,332.02 ns | 5,231.996 ns | 286.783 ns | 3,034.82 |   10.94 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    147.66 ns |    18.617 ns |   1.020 ns |     5.02 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     83.02 ns |    26.956 ns |   1.478 ns |     2.82 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
