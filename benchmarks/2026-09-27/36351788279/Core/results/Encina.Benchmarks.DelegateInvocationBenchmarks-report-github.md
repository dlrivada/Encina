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
| CompiledDelegate        |     37.23 ns |     4.208 ns |   0.231 ns |     1.24 |    0.03 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     30.04 ns |    14.976 ns |   0.821 ns |     1.00 |    0.03 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 91,906.18 ns | 2,445.087 ns | 134.023 ns | 3,061.09 |   73.14 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    137.08 ns |    10.732 ns |   0.588 ns |     4.57 |    0.11 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     78.86 ns |     5.895 ns |   0.323 ns |     2.63 |    0.06 | 0.0105 |      - |     176 B |        1.57 |
