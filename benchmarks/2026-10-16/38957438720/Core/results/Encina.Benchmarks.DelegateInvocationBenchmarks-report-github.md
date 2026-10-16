```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     23.27 ns |    53.519 ns |   2.934 ns |     1.39 |    0.17 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     16.75 ns |    17.175 ns |   0.941 ns |     1.00 |    0.07 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 17,588.66 ns | 5,123.458 ns | 280.834 ns | 1,052.51 |   51.75 | 0.3052 | 0.2747 |    5287 B |       47.21 |
| GenericTypeConstruction |     67.42 ns |    10.126 ns |   0.555 ns |     4.03 |    0.19 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     45.85 ns |     7.798 ns |   0.427 ns |     2.74 |    0.13 | 0.0105 |      - |     176 B |        1.57 |
