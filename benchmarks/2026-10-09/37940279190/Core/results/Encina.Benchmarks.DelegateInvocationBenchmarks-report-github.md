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
| CompiledDelegate        |     38.20 ns |    17.033 ns |   0.934 ns |     1.28 |    0.03 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     29.80 ns |     4.873 ns |   0.267 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 94,226.55 ns | 9,686.904 ns | 530.972 ns | 3,161.84 |   29.04 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    138.08 ns |     2.423 ns |   0.133 ns |     4.63 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     79.89 ns |    34.428 ns |   1.887 ns |     2.68 |    0.06 | 0.0105 |      - |     176 B |        1.57 |
