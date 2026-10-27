```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     19.11 ns |    10.057 ns |   0.551 ns |     1.21 |    0.04 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     15.84 ns |     8.263 ns |   0.453 ns |     1.00 |    0.03 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 17,280.19 ns | 9,689.499 ns | 531.114 ns | 1,091.27 |   39.44 | 0.3052 | 0.2747 |    5287 B |       47.21 |
| GenericTypeConstruction |     66.91 ns |    47.083 ns |   2.581 ns |     4.23 |    0.17 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     43.61 ns |    14.247 ns |   0.781 ns |     2.75 |    0.08 | 0.0105 |      - |     176 B |        1.57 |
