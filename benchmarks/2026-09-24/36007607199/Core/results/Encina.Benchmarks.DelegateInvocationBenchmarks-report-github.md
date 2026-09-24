```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.89GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     37.91 ns |     8.164 ns |   0.448 ns |     1.25 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     30.43 ns |     8.284 ns |   0.454 ns |     1.00 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 92,056.03 ns | 4,037.665 ns | 221.318 ns | 3,025.77 |   39.93 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    141.30 ns |    23.797 ns |   1.304 ns |     4.64 |    0.07 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     90.65 ns |    21.860 ns |   1.198 ns |     2.98 |    0.05 | 0.0105 |      - |     176 B |        1.57 |
