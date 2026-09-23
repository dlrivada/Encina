```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.17GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     23.62 ns |     4.791 ns |   0.263 ns |     1.50 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     15.72 ns |     1.388 ns |   0.076 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 16,822.64 ns | 9,809.810 ns | 537.709 ns | 1,070.44 |   29.97 | 0.3052 | 0.2747 |    5287 B |       47.21 |
| GenericTypeConstruction |     67.36 ns |    11.779 ns |   0.646 ns |     4.29 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     44.17 ns |    32.030 ns |   1.756 ns |     2.81 |    0.10 | 0.0105 |      - |     176 B |        1.57 |
