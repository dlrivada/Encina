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
| CompiledDelegate        |     36.13 ns |     5.726 ns |   0.314 ns |     1.25 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     28.85 ns |     2.508 ns |   0.137 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 91,557.81 ns | 4,022.967 ns | 220.512 ns | 3,173.30 |   14.66 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    135.97 ns |    10.060 ns |   0.551 ns |     4.71 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     76.25 ns |     8.190 ns |   0.449 ns |     2.64 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
