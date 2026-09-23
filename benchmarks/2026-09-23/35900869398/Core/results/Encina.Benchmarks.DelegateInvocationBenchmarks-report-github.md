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
| CompiledDelegate        |     37.22 ns |    11.119 ns |   0.609 ns |     1.25 |    0.03 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     29.83 ns |    13.197 ns |   0.723 ns |     1.00 |    0.03 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 90,815.12 ns | 9,806.899 ns | 537.549 ns | 3,045.20 |   65.04 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    141.88 ns |    29.259 ns |   1.604 ns |     4.76 |    0.11 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     78.01 ns |     6.746 ns |   0.370 ns |     2.62 |    0.06 | 0.0105 |      - |     176 B |        1.57 |
