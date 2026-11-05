```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.28GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean         | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-------------:|-------------:|------------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |   7,891.8 ns |  2,530.97 ns |   138.73 ns |  1.00 |    0.02 |    5 |  0.1984 | 0.0610 |    3496 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |   8,105.0 ns |  6,207.16 ns |   340.24 ns |  1.03 |    0.04 |    5 |  0.1984 | 0.0610 |    3568 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2,155.8 ns |    335.17 ns |    18.37 ns |  0.27 |    0.00 |    3 |  0.0305 |      - |     512 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1,181.4 ns |     97.27 ns |     5.33 ns |  0.15 |    0.00 |    2 |  0.0229 |      - |     392 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   3,808.9 ns |  1,497.05 ns |    82.06 ns |  0.48 |    0.01 |    4 |  0.0610 |      - |    1048 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |     794.3 ns |     20.81 ns |     1.14 ns |  0.10 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.27 |
| &#39;Risk assessment: 100-record dataset&#39;     | 194,771.4 ns | 58,208.96 ns | 3,190.63 ns | 24.69 |    0.51 |    6 | 17.3340 | 0.4883 |  291296 B |       83.32 |
